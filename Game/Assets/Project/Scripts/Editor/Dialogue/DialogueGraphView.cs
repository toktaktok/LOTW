#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Project.Scripts.Data.Table;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor.Dialogue
{
    /// <summary>
    /// 한 블록(100 단위 DataId)의 Dialogue 행을 노드와 연결선으로 보여 주고 편집합니다.
    /// 연결을 끌거나 지우면 NextId / ChoiceIds 가, 노드를 지우면 행이 DialogueEdits 로 바뀌고 OnEdited 가 불립니다.
    /// 포트에서 끈 연결을 빈 곳에 놓으면 거기에 새 행을 만들어 잇습니다 (선택지 칸이면 플레이어 행).
    /// 배치: Positions 에 좌표가 있으면 그대로, 없으면 시작 행부터 너비 우선으로 단계를 매겨 열 = 단계로 쌓습니다.
    /// 블록 밖을 가리키는 연결은 선 없이 포트 이름의 DataId 로만 보입니다.
    /// </summary>
    public class DialogueGraphView : GraphView, IEdgeConnectorListener
    {
        private readonly Dictionary<int, DialogueNodeView> _nodes = new();
        private DialogueTableSource _source;
        private int _block;
        private bool _isBuilding;

        public DialogueGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            graphViewChanged = OnGraphViewChanged;
        }

        public event Action<int> OnStartRequested;
        public event Action<int> OnNodeSelected;
        public event Action OnEdited;
        public event Action OnMoved;

        /// <summary>DataId → 노드 위치. 창이 파일에서 읽고, 노드를 옮기면 갱신됩니다.</summary>
        public Dictionary<int, Vector2> Positions { get; set; } = new();

        public void Show(DialogueTableSource source, int block, bool frame)
        {
            _isBuilding = true;
            _source = source;
            _block = block;
            DeleteElements(graphElements.ToList());
            _nodes.Clear();

            var choiceIds = new HashSet<int>();
            foreach(DialogueData line in source.GetBlockLines(block))
            {
                if(line.choiceIds != null && !string.IsNullOrEmpty(line.text))
                    choiceIds.UnionWith(line.choiceIds);
            }

            foreach(DialogueData line in source.GetBlockLines(block))
            {
                var node = new DialogueNodeView(line, source, choiceIds.Contains(line.dataId), id => OnStartRequested?.Invoke(id), this);
                _nodes[line.dataId] = node;
                AddElement(node);
            }

            foreach(DialogueNodeView node in _nodes.Values)
            {
                DialogueData line = node.Line;
                Connect(node.GetOutputPort(DialogueEdits.NextSlot), line.nextId);
                for(int i = 0; i < (line.choiceIds?.Length ?? 0); i++)
                    Connect(node.GetOutputPort(i), line.choiceIds[i]);
            }

            Layout(source.GetBlockRoots(block));
            if(frame)
                schedule.Execute(() => FrameAll());
            _isBuilding = false;
        }

        public void SetCurrent(int dataId)
        {
            foreach(DialogueNodeView node in _nodes.Values)
                node.SetCurrent(node.Line.dataId == dataId);
        }

        public void Select(int dataId)
        {
            if(_nodes.TryGetValue(dataId, out DialogueNodeView node))
                AddToSelection(node);
        }

        /// <summary>보이는 영역 가운데(또는 지정 위치)에 새 행을 만듭니다. isRouter 면 대사 없는 분기 행. 블록이 차면 false.</summary>
        public bool AddLine(Vector2? position = null, bool isRouter = false)
        {
            int id = DialogueEdits.AddLine(_source, _block, isRouter);
            if(id < 0)
                return false;

            Positions[id] = position ?? contentViewContainer.WorldToLocal(worldBound.center);
            NotifyAdded(id);
            return true;
        }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            OnGraphViewChanged(new GraphViewChange { edgesToCreate = new List<Edge> { edge } });
        }

        /// <summary>끈 연결을 빈 곳에 놓음: 출력 포트에서 끌었으면 새 행을 그 슬롯에, 입력 포트에서 끌었으면 새 행의 다음으로 잇습니다.</summary>
        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            Port from = edge.output ?? edge.input;
            if(from == null)
                return;
            int fromId = GetId(from);
            bool isOutput = from.direction == Direction.Output;
            int slot = isOutput ? (int)from.userData : DialogueEdits.NextSlot;
            bool isPlayer = slot != DialogueEdits.NextSlot && !string.IsNullOrEmpty(_source.GetLine(fromId).text);

            int id = DialogueEdits.AddLine(_source, _block, false, isPlayer);
            if(id < 0)
                return;
            if(isOutput)
                DialogueEdits.Connect(_source, fromId, slot, id);
            else
                DialogueEdits.Connect(_source, id, DialogueEdits.NextSlot, fromId);
            Positions[id] = contentViewContainer.WorldToLocal(position);
            // 드래그 처리 중에는 그래프를 지울 수 없어 다음 프레임에
            schedule.Execute(() => NotifyAdded(id));
        }

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            if(!_isBuilding && selectable is DialogueNodeView node)
                OnNodeSelected?.Invoke(node.Line.dataId);
        }

        public override void ClearSelection()
        {
            base.ClearSelection();
            if(!_isBuilding)
                OnNodeSelected?.Invoke(-1);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.Where(p => p.direction != startPort.direction && p.node != startPort.node).ToList();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if(evt.target != this)
                return;
            Vector2 position = contentViewContainer.WorldToLocal(evt.mousePosition);
            evt.menu.AppendAction("대사 추가", _ => AddLine(position));
            evt.menu.AppendAction("분기 추가", _ => AddLine(position, true));
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if(_isBuilding)
                return change;

            bool edited = false;
            // 새 연결을 먼저 반영: 이미 연결된 포트에서 다시 끌면 GraphView 가 옛 연결 삭제를 같이 보내는데,
            // 새 값을 먼저 써 두면 옛 연결 삭제는 대상이 달라 아무것도 하지 않음
            foreach(Edge edge in change.edgesToCreate ?? new List<Edge>())
            {
                DialogueEdits.Connect(_source, GetId(edge.output), (int)edge.output.userData, GetId(edge.input));
                edited = true;
            }

            var deleted = new List<int>();
            foreach(GraphElement element in change.elementsToRemove ?? new List<GraphElement>())
            {
                if(element is Edge edge && edge.output != null && edge.input != null)
                    DialogueEdits.Disconnect(_source, GetId(edge.output), (int)edge.output.userData, GetId(edge.input));
                else if(element is DialogueNodeView node)
                    deleted.Add(node.Line.dataId);
                edited = true;
            }
            if(deleted.Count > 0)
                DialogueEdits.DeleteLines(_source, deleted);

            if(change.movedElements != null)
            {
                foreach(DialogueNodeView node in change.movedElements.OfType<DialogueNodeView>())
                    Positions[node.Line.dataId] = node.GetPosition().position;
                OnMoved?.Invoke();
            }

            // 포트 이름과 "+ 선택" 포트를 다시 그리려고 그래프를 새로 만듦. 이벤트 처리 중에는 지울 수 없어 다음 프레임에
            if(edited)
                schedule.Execute(() => OnEdited?.Invoke());
            return change;
        }

        private void NotifyAdded(int id)
        {
            OnMoved?.Invoke();
            OnEdited?.Invoke();
            OnNodeSelected?.Invoke(id);
        }

        private static int GetId(Port port) => ((DialogueNodeView)port.node).Line.dataId;

        private void Connect(Port output, int targetId)
        {
            if(_nodes.TryGetValue(targetId, out DialogueNodeView target))
                AddElement(output.ConnectTo(target.InputPort));
        }

        private void Layout(List<DialogueData> roots)
        {
            var depth = new Dictionary<int, int>();
            var queue = new Queue<int>();
            foreach(DialogueData root in roots)
            {
                depth[root.dataId] = 0;
                queue.Enqueue(root.dataId);
            }

            while(queue.Count > 0)
            {
                DialogueNodeView node = _nodes[queue.Dequeue()];
                foreach(int next in GetTargets(node.Line))
                {
                    if(!_nodes.ContainsKey(next) || depth.ContainsKey(next))
                        continue;
                    depth[next] = depth[node.Line.dataId] + 1;
                    queue.Enqueue(next);
                }
            }

            // 시작 행에서 닿지 않는 행은 마지막 열 다음에 둠
            int lastColumn = 0;
            foreach(int d in depth.Values)
                lastColumn = Mathf.Max(lastColumn, d);

            var rowsPerColumn = new Dictionary<int, int>();
            foreach(DialogueNodeView node in _nodes.Values)
            {
                int id = node.Line.dataId;
                if(!Positions.TryGetValue(id, out Vector2 position))
                {
                    int column = depth.TryGetValue(id, out int d) ? d : lastColumn + 1;
                    rowsPerColumn.TryGetValue(column, out int row);
                    rowsPerColumn[column] = row + 1;
                    position = new Vector2(column * ToolDefines.DialogueNodeColumnWidth, row * ToolDefines.DialogueNodeRowHeight);
                    // 다시 그려도 자리가 바뀌지 않게 기억함 (파일에는 노드를 옮기거나 저장할 때 씀)
                    Positions[id] = position;
                }
                node.SetPosition(new Rect(position, Vector2.zero));
            }
        }

        private static IEnumerable<int> GetTargets(DialogueData line)
        {
            if(line.choiceIds != null)
            {
                foreach(int id in line.choiceIds)
                    yield return id;
            }
            if(line.nextId >= 0)
                yield return line.nextId;
        }
    }
}
#endif
