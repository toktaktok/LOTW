using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

using Project.Scripts.Data;
using Project.Scripts.System.World;
using Project.Scripts.Content.World;
using Project.Scripts.Content.Story;
using Project.Scripts.Content.UI;
using Project.Scripts.Core.Managers;
using Project.Scripts.Framework;
using Project.Scripts.Framework.Managers;

namespace Project.Scripts.Content.Controller
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Character currentCharacter;

        [Header("Rail System")]
        [SerializeField] private RailNode currentBaseNode;

        [SerializeField] private RailNode currentTargetNode;

        private StateMachine<PlayerController> _fsm;
        private PlayerControls _controls;
        
        private Vector3 _cachedPathVector;
        private Vector3 _cachedPathDir;
        private float _cachedPathSqrLength;

        // 진행 방향 기억: 마지막으로 떠나온 노드와, 그때의 입력 부호(0 = 아직 없음)
        private RailNode _cameFrom;
        private int _moveSign;

        private bool _isFreeMoving = false;
        
        public Character CurrentCharacter => currentCharacter;

        private void OnEnable() => _controls.Enable();
        private void OnDisable() => _controls.Disable();

        private void Awake()
        {
            _controls = new PlayerControls();
            _fsm = new StateMachine<PlayerController>(this);
        }
        private void Start()
        {
            if(currentBaseNode == null && currentCharacter != null)
                currentBaseNode = RailNode.FindNearest(currentCharacter.Position);

            if(currentBaseNode != null)
            {
                if(currentCharacter != null)
                    currentCharacter.Warp(currentBaseNode.transform.position);

                if(currentTargetNode == null)
                    currentTargetNode = currentBaseNode;

                RecalculatePathData();
            }

            TransitionToIdle();

            UIManager.Instance.PushPage<HudUI>(UILayer.HUD);
        }
        private void Update()
        {
            if(_isFreeMoving || UIManager.Instance.HasBlockingPage || SequencePlayer.IsPlaying)
                return;
            
            _fsm.Update();
        }

        public void SwitchPath(RailNode targetNode)
        {
            if(targetNode == null)
                return;
            
            currentCharacter.Warp(targetNode.transform.position);
            currentBaseNode = targetNode;

            RailNode foundTargetNode = (targetNode.neighbors.Count > 0) ? targetNode.neighbors[0] : targetNode;
            currentTargetNode = foundTargetNode;
            ResetMoveDirection();

            RecalculatePathData();
            TransitionToIdle();
        }
        public void MoveToAndSwitchPath(RailNode targetNode, CinemachineCamera newCam, float camDuration)
        {
            StartCoroutine(MoveAndSwitchRoutine(targetNode, newCam, camDuration));
        }

        /// <summary>
        /// 씬 전환 후 SceneTransitionManager가 호출합니다.
        /// 플레이어를 지정 위치로 즉시 이동하고 Rail 시작 노드를 설정합니다.
        /// </summary>
        public void WarpToEntrance(Vector3 position, RailNode startNode)
        {
            if(currentCharacter != null)
                currentCharacter.Warp(position);

            if(startNode == null)
                startNode = RailNode.FindNearest(position);

            if(startNode != null)
            {
                currentBaseNode = startNode;
                currentTargetNode = startNode.neighbors.Count > 0 ? startNode.neighbors[0] : startNode;
                ResetMoveDirection();
                RecalculatePathData();
            }

            TransitionToIdle();
        }

        private IEnumerator MoveAndSwitchRoutine(RailNode targetNode, CinemachineCamera newCam, float camDuration)
        {
            TransitionToIdle();
            
            _isFreeMoving = true;
            currentCharacter.MoveTo(targetNode.transform.position);

            float timeout = WorldDefines.MoveAndSwitchTimeout;
            float arrivalSqr = WorldDefines.RailArrivalDistance * WorldDefines.RailArrivalDistance;
            while(HorizontalSqrDistance(currentCharacter.Position, targetNode.transform.position) > arrivalSqr && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if(timeout <= 0)
                Debug.LogWarning($"[PlayerController] MoveAndSwitchRoutine timed out moving to {targetNode.name}");

            // 남은 오차(또는 타임아웃)로 레일 밖에 서 있으면 다음 이동 입력 때 레일로 순간이동하므로 노드에 맞춘다
            currentCharacter.Warp(targetNode.transform.position);
            currentBaseNode = targetNode;

            if(targetNode.neighbors.Count > 0)
                currentTargetNode = targetNode.neighbors[0];
            else
                currentTargetNode = targetNode;
            ResetMoveDirection();

            TransitionToIdle();
            RecalculatePathData();
            if(newCam != null)
                CameraManager.Instance.SwitchCamera(newCam, camDuration);
            _isFreeMoving = false;
        }

        private static float HorizontalSqrDistance(Vector3 a, Vector3 b)
        {
            Vector3 delta = a - b;
            delta.y = 0f;
            return delta.sqrMagnitude;
        }

        private Vector2 GetMoveInput()
        {
            return _controls.Player.Move.ReadValue<Vector2>();
        }
        
        private void OnIdle()
        {
            float inputX = GetMoveInput().x;
            if(Mathf.Abs(inputX) > WorldDefines.InputThreshold)
            {
                TransitionToMove();
            }
        }
        private void TransitionToIdle()
        {
            _fsm.ChangeState("Idle",
                onEnter: () => { currentCharacter.StopNavigation(); },
                onUpdate: OnIdle
            );
        }
        private void TransitionToMove()
        {
            _fsm.ChangeState("Move",
                onEnter: null,
                onUpdate: () =>
                {
                    float inputX =  GetMoveInput().x;

                    if(Mathf.Abs(inputX) > WorldDefines.InputThreshold)
                        MoveOnPath(inputX);
                    else
                        TransitionToIdle();
                }
            );
        }
        private void MoveOnPath(float inputX)
        {
            if(currentCharacter==null || currentBaseNode==null)
                return;

            var cam = CameraManager.Instance.GetCurrentCamera();
            if(cam == null)
                return;

            Vector3 camRight = cam.transform.right;
            camRight.y = 0;
            Vector3 inputWorldDir = (camRight * inputX).normalized;
            int inputSign = inputX > 0f ? 1 : -1;

            if(currentBaseNode == currentTargetNode)
            {
                RailNode next = SelectNextNode(inputSign, inputWorldDir);
                if(next == null)
                    return;

                currentTargetNode = next;
                _moveSign = inputSign;
                RecalculatePathData();
            }
            else if(_moveSign == 0)
            {
                // 워프 직후처럼 진행 방향이 없으면 카메라 기준으로 정함
                if(Vector3.Dot(inputWorldDir, _cachedPathDir) < WorldDefines.DirectionReversalThreshold)
                    SwapBaseAndTarget();
                _moveSign = inputSign;
            }
            else if(inputSign != _moveSign)
            {
                // 진행 중에는 카메라 각도와 무관하게 입력 부호가 바뀌면 되돌아감
                SwapBaseAndTarget();
                _moveSign = inputSign;
            }

            Vector3 basePos = currentBaseNode.transform.position;
            Vector3 currentPos = currentCharacter.Position;

            // Project current position onto the rail and advance by moveSpeed
            float d = Vector3.Dot(currentPos - basePos, _cachedPathDir);
            d = Mathf.Max(0f, d);
            d += Time.deltaTime * currentCharacter.MoveSpeed;

            float railLength = Mathf.Sqrt(_cachedPathSqrLength);

            if(d >= railLength)
            {
                Vector3 snapPos = currentTargetNode.transform.position;
                snapPos.y = currentPos.y;
                currentCharacter.MoveOnRail(snapPos);
                _cameFrom = currentBaseNode;
                currentBaseNode = currentTargetNode;
                _cachedPathVector = Vector3.zero;
                _cachedPathSqrLength = 1f;
                _cachedPathDir = Vector3.zero;
                return;
            }

            Vector3 newRailPos = basePos + _cachedPathDir * d;
            newRailPos.y = currentPos.y;
            currentCharacter.MoveOnRail(newRailPos);
        }

        private void RecalculatePathData()
        {
            _cachedPathVector = currentTargetNode.transform.position - currentBaseNode.transform.position;
            _cachedPathVector.y = 0f;
            _cachedPathSqrLength = _cachedPathVector.sqrMagnitude;

            if(_cachedPathSqrLength < 0.001f)
                _cachedPathSqrLength = 1f;

            _cachedPathDir = _cachedPathVector.normalized;
        }

        private void SwapBaseAndTarget()
        {
            (currentBaseNode, currentTargetNode) = (currentTargetNode, currentBaseNode);
            RecalculatePathData();
        }

        private void ResetMoveDirection()
        {
            _cameFrom = null;
            _moveSign = 0;
        }

        /// <summary>
        /// 노드에 도착한 상태에서 다음 노드를 고릅니다. 같은 방향 입력이면 온 노드가 아닌 이웃으로 계속 가고,
        /// 반대면 온 노드로 되돌아갑니다. 카메라와 거의 직각인 레일도 지나갈 수 있도록 카메라 방향 판정은 첫 출발에만 씁니다.
        /// </summary>
        private RailNode SelectNextNode(int inputSign, Vector3 inputWorldDir)
        {
            if(_cameFrom != null && _moveSign != 0)
                return inputSign == _moveSign ? currentBaseNode.GetOtherNeighbor(_cameFrom) : _cameFrom;

            return FindNeighborByDirection(inputWorldDir);
        }

        private RailNode FindNeighborByDirection(Vector3 desiredDir)
        {
            RailNode bestNode = null;
            float maxDot = 0.1f;

            foreach(var neighbor in currentBaseNode.neighbors)
            {
                if(neighbor == null)
                    continue;

                Vector3 dirToNode = (neighbor.transform.position - currentBaseNode.transform.position).normalized;
                float dot = Vector3.Dot(desiredDir, dirToNode);

                if(dot > maxDot)
                {
                    maxDot = dot;
                    bestNode = neighbor;
                }
            }
            return bestNode;
        }
    }
}