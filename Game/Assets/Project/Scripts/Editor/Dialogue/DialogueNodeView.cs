#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Project.Scripts.Data.Table;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor.Dialogue
{
    /// <summary>
    /// Dialogue 행 하나를 표시하는 노드. 출력 포트는 "다음"(NextId)과 ChoiceIds 마다 하나씩,
    /// 선택지가 최대 개수보다 적으면 "+ 선택" 포트가 더 있습니다. 포트 userData 는 DialogueEdits 연결 슬롯입니다.
    /// 포트에서 끈 연결은 edgeListener(그래프)가 받습니다 (빈 곳에 놓으면 새 행).
    /// 제목 색: 분기 행, 선택지 행, 종료 행을 구분합니다.
    /// </summary>
    public class DialogueNodeView : Node
    {
        private readonly Action<int> _onStartHere;
        private readonly IEdgeConnectorListener _edgeListener;

        public DialogueNodeView(DialogueData line, DialogueTableSource source, bool isChoice, Action<int> onStartHere, IEdgeConnectorListener edgeListener)
        {
            Line = line;
            _onStartHere = onStartHere;
            _edgeListener = edgeListener;
            style.width = ToolDefines.DialogueNodeWidth;

            // 후보를 아직 연결하지 않은 분기 행도 분기로 보여 줌 (게임 판정 DialogueCommands.IsRouter 는 후보가 있어야 참)
            bool isRouter = string.IsNullOrEmpty(line.text);
            int choiceCount = line.choiceIds?.Length ?? 0;
            string speaker = source.Resolve(string.IsNullOrEmpty(line.speakerName) ? line.speakerId : line.speakerName);
            title = isRouter ? $"{line.dataId}  분기" : $"{line.dataId}  {speaker}";

            if(isRouter)
                titleContainer.style.backgroundColor = ToolDefines.DialogueRouterColor;
            else if(isChoice)
                titleContainer.style.backgroundColor = ToolDefines.DialogueChoiceColor;
            else if(line.nextId < 0 && choiceCount == 0)
                titleContainer.style.backgroundColor = ToolDefines.DialogueEndColor;

            InputPort = CreatePort(Direction.Input, Port.Capacity.Multi);
            InputPort.portName = string.Empty;
            inputContainer.Add(InputPort);

            AddOutput(line.nextId >= 0 ? $"다음 {line.nextId}" : "다음", DialogueEdits.NextSlot);
            for(int i = 0; i < choiceCount; i++)
                AddOutput($"{(isRouter ? "후보" : "선택")} {line.choiceIds[i]}", i);
            if(choiceCount < ToolDefines.DialogueMaxChoices)
                AddOutput(isRouter ? "+ 후보" : "+ 선택", choiceCount);

            if(!isRouter)
                AddBody(source.Resolve(line.text), Color.white);
            if(!string.IsNullOrEmpty(line.conditions))
                AddBody($"조건  {line.conditions}", ToolDefines.DialogueBlockedColor);
            if(!string.IsNullOrEmpty(line.actions))
                AddBody($"실행  {line.actions}", ToolDefines.DialogueCurrentColor);

            RefreshExpandedState();
            RefreshPorts();
        }

        public DialogueData Line { get; }
        public Port InputPort { get; }

        /// <summary>연결 슬롯의 출력 포트 (-1 = 다음, 0 이상 = 선택지).</summary>
        public Port GetOutputPort(int slot)
        {
            return outputContainer.Query<Port>().Where(p => (int)p.userData == slot).First();
        }

        public void SetCurrent(bool isCurrent)
        {
            float width = isCurrent ? ToolDefines.DialogueNodeBorderWidth : 0f;
            style.borderTopWidth = width;
            style.borderBottomWidth = width;
            style.borderLeftWidth = width;
            style.borderRightWidth = width;
            Color color = ToolDefines.DialogueCurrentColor;
            style.borderTopColor = color;
            style.borderBottomColor = color;
            style.borderLeftColor = color;
            style.borderRightColor = color;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("여기서 시뮬레이션 시작", _ => _onStartHere?.Invoke(Line.dataId));
            evt.menu.AppendAction("삭제", _ => GetFirstAncestorOfType<GraphView>().DeleteElements(new GraphElement[] { this }));
            evt.StopPropagation();
        }

        private void AddOutput(string label, int slot)
        {
            Port port = CreatePort(Direction.Output, Port.Capacity.Single);
            port.portName = label;
            port.userData = slot;
            outputContainer.Add(port);
        }

        private Port CreatePort(Direction direction, Port.Capacity capacity)
        {
            Port port = InstantiatePort(Orientation.Horizontal, direction, capacity, typeof(int));
            // 기본 연결 리스너는 빈 곳에 놓으면 아무것도 안 하므로 교체
            port.RemoveManipulator(port.edgeConnector);
            port.AddManipulator(new EdgeConnector<Edge>(_edgeListener));
            return port;
        }

        private void AddBody(string text, Color color)
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = color;
            label.style.paddingLeft = ToolDefines.DialogueNodePadding;
            label.style.paddingRight = ToolDefines.DialogueNodePadding;
            label.style.paddingTop = ToolDefines.DialogueNodePadding * 0.5f;
            label.style.paddingBottom = ToolDefines.DialogueNodePadding * 0.5f;
            extensionContainer.Add(label);
        }
    }
}
#endif
