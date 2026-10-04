using UnityEngine;
using UnityEngine.InputSystem;
using Project.Scripts.Data;
using Project.Scripts.Content.World;
using Project.Scripts.System.World.Map;

namespace Project.Scripts.Content.Controller
{
    /// <summary>
    /// 플레이 모드 안에서 RailNode 그래프와 배치물을 편집하고 실제 맵(JSON)으로 저장하는 인-플레이 에디터.
    /// 입력은 New Input System 디바이스(Keyboard/Mouse)를 직접 폴링하므로 InputActions 에셋 의존이 없습니다.
    /// 자기 부트스트랩(에디터 전용)이라 씬 배치/에셋 설정 없이 F2로 즉시 켜집니다.
    ///
    /// 조작: F2 토글 | 1 배치 / 2 선택·이동 / 3 연결 / 4 삭제 | 좌클릭 실행 | F5 저장 | F6 다시 로드 | P 기본맵 승격
    /// 편집 중에도 PlayerController는 유지되어 방금 만든 레일 위를 플레이어가 이동할 수 있습니다.
    /// </summary>
    public class MapEditorController : MonoBehaviour
    {
        private enum Mode { Place, SelectMove, Connect, Delete }

        private readonly MapModel _model = new MapModel { mapName = "user" };
        private readonly MapBuilder _builder = new MapBuilder();

        private bool _editing;
        private Mode _mode = Mode.Place;
        private int _selectedNodeId = -1;
        private int _connectFirst = -1;
        private bool _dragging;
        private string _message = "";

        private PlayerController _player;
        private PlayerInteractor _interactor;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            GameObject go = new GameObject("[MapEditor]");
            go.AddComponent<MapEditorController>();
            DontDestroyOnLoad(go);
        }
#endif

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if(kb == null)
                return;

            if(kb.f2Key.wasPressedThisFrame)
            {
                SetEditing(!_editing);
                return;
            }
            if(!_editing)
                return;

            if(kb.digit1Key.wasPressedThisFrame)
                _mode = Mode.Place;
            else if(kb.digit2Key.wasPressedThisFrame)
                _mode = Mode.SelectMove;
            else if(kb.digit3Key.wasPressedThisFrame) { _mode = Mode.Connect; _connectFirst = -1; }
            else if(kb.digit4Key.wasPressedThisFrame)
                _mode = Mode.Delete;

            if(kb.f5Key.wasPressedThisFrame)
                Save();
            if(kb.f6Key.wasPressedThisFrame)
                Reload();
            if(kb.pKey.wasPressedThisFrame)
                MapSerializer.RequestPromote();

            HandleMouse();
        }

        private void SetEditing(bool on)
        {
            _editing = on;

            if(_player == null)
                _player = FindFirstObjectByType<PlayerController>();
            if(_interactor == null)
                _interactor = FindFirstObjectByType<PlayerInteractor>();

            // 상호작용 팝업만 끄고, 플레이어 이동은 유지해 새로 만든 레일을 걸어볼 수 있게 한다.
            if(_interactor != null)
                _interactor.enabled = !on;

            if(on)
            {
                _builder.Build(_model);
                RepointPlayer();
                _message = "Map edit ON";
            }
            else
            {
                _message = "";
            }
        }

        private void HandleMouse()
        {
            Mouse mouse = Mouse.current;
            if(mouse == null)
                return;

            if(!TryGroundPoint(mouse.position.ReadValue(), out Vector3 point))
                return;

            switch(_mode)
            {
                case Mode.Place:
                    if(mouse.leftButton.wasPressedThisFrame)
                        PlaceNode(point);
                    break;

                case Mode.SelectMove:
                    if(mouse.leftButton.wasPressedThisFrame)
                    {
                        _selectedNodeId = PickNode(point);
                        _dragging = _selectedNodeId >= 0;
                    }
                    if(_dragging && mouse.leftButton.isPressed && _selectedNodeId >= 0)
                        MoveNode(_selectedNodeId, point);
                    if(!mouse.leftButton.isPressed)
                        _dragging = false;
                    break;

                case Mode.Connect:
                    if(mouse.leftButton.wasPressedThisFrame)
                        ConnectPick(point);
                    break;

                case Mode.Delete:
                    if(mouse.leftButton.wasPressedThisFrame)
                        DeletePick(point);
                    break;
            }
        }

        #region Edit ops (model first, then rebuild)

        private void PlaceNode(Vector3 point)
        {
            int id = _model.AddNode(point, Color.cyan, MapDefines.DefaultNodeRadius);

            // 선택된 노드가 있고 여유가 있으면 새 노드를 자동 연결한다.
            if(_selectedNodeId >= 0 && _model.NeighborCount(_selectedNodeId) < 2)
                _model.AddEdge(_selectedNodeId, id);

            _selectedNodeId = id;
            Rebuild();
            _message = $"Placed node {id}";
        }

        private void MoveNode(int id, Vector3 point)
        {
            if(!_model.MoveNode(id, point))
                return;
            if(_builder.NodeMap.TryGetValue(id, out RailNode rn) && rn != null)
                rn.transform.position = point;
        }

        private void ConnectPick(Vector3 point)
        {
            int id = PickNode(point);
            if(id < 0)
                return;

            if(_connectFirst < 0)
            {
                _connectFirst = id;
                _message = $"Connect from {id}";
                return;
            }

            if(_model.AddEdge(_connectFirst, id))
            {
                Rebuild();
                _message = $"Connected {_connectFirst}-{id}";
            }
            else
            {
                _message = $"Connect {_connectFirst}-{id} rejected (full/dup)";
            }
            _connectFirst = -1;
        }

        private void DeletePick(Vector3 point)
        {
            int id = PickNode(point);
            if(id < 0)
                return;

            _model.DeleteNode(id);
            if(_selectedNodeId == id)
                _selectedNodeId = -1;
            Rebuild();
            _message = $"Deleted node {id}";
        }

        private void Save()
        {
            string path = MapSerializer.Save(_model.ToData(), _model.mapName);
            _message = $"Saved -> {path}";
        }

        private void Reload()
        {
            string path = MapSerializer.ResolveMapPath(_model.mapName);
            var data = MapSerializer.Load(path);
            if(data == null)
            {
                _message = "Reload: no saved map";
                return;
            }

            _model.CopyFrom(MapModel.FromData(data));
            _selectedNodeId = -1;
            _connectFirst = -1;
            Rebuild();
            _message = "Reloaded from disk";
        }

        #endregion

        private void Rebuild()
        {
            _builder.Build(_model);
            RepointPlayer();
        }

        private void RepointPlayer()
        {
            RailNode first = _builder.FirstNode();
            if(first != null && _player != null)
                _player.WarpToEntrance(first.transform.position, first);
        }

        private int PickNode(Vector3 point)
        {
            int best = -1;
            float bestDist = MapDefines.NodePickRadius;
            foreach(var kv in _builder.NodeMap)
            {
                if(kv.Value == null)
                    continue;
                float d = Vector3.Distance(point, kv.Value.transform.position);
                if(d < bestDist)
                {
                    bestDist = d;
                    best = kv.Key;
                }
            }
            return best;
        }

        private bool TryGroundPoint(Vector2 screenPos, out Vector3 point)
        {
            point = Vector3.zero;
            Camera cam = Camera.main;
            if(cam == null)
                return false;

            Ray ray = cam.ScreenPointToRay(screenPos);
            Plane ground = new Plane(Vector3.up, new Vector3(0f, MapDefines.GroundPlaneY, 0f));
            if(ground.Raycast(ray, out float enter) && enter <= MapDefines.PlacementRayMaxDistance)
            {
                point = ray.GetPoint(enter);
                return true;
            }
            return false;
        }

        private void OnGUI()
        {
            if(!_editing)
                return;

            GUILayout.BeginArea(new Rect(10, 10, 340, 200), GUI.skin.box);
            GUILayout.Label("MAP EDIT  (F2 to exit)");
            GUILayout.Label($"Mode: {_mode}   [1]Place [2]Move [3]Connect [4]Delete");
            GUILayout.Label($"Nodes: {_model.nodes.Count}   Edges: {_model.edges.Count}");
            string sel = _selectedNodeId >= 0
                ? $"{_selectedNodeId} ({_model.NeighborCount(_selectedNodeId)}/2)"
                : "none";
            GUILayout.Label($"Selected: {sel}");
            GUILayout.Label("[F5]Save  [F6]Reload  [P]Promote default");
            if(!string.IsNullOrEmpty(_message))
                GUILayout.Label(_message);
            GUILayout.EndArea();
        }
    }
}
