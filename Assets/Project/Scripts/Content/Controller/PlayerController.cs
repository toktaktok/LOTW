using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

using Project.Scripts.Core;
using Project.Scripts.Data;
using Project.Scripts.System.World;
using Project.Scripts.Content.World;
using Project.Scripts.Core.Managers;
using Unity.Mathematics;

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
        private float _cachedPathSqrLength;
        private Vector3 _cachedPathDir;

        private bool _isFreeMoving = false;
        
        private void OnEnable() => _controls.Enable();
        private void OnDisable() => _controls.Disable();

        private void Awake()
        {
            _controls = new PlayerControls();
            _fsm = new StateMachine<PlayerController>(this);
        }
        private void Start()
        {
            if(currentCharacter != null)
            {
                currentCharacter.Init();
                currentCharacter.Warp(currentBaseNode.transform.position);
            }

            if(currentTargetNode == null)
                currentTargetNode = currentBaseNode;

            RecalculatePathData();
            TransitionToIdle();
        }
        private void Update()
        {
            if(_isFreeMoving)
                return;
            
            _fsm.Update();
        }

        public void SwitchPath(RailNode targetNode)
        {
            if(targetNode == null)
                return;
            
            currentCharacter.Warp(targetNode.transform.position);
            currentBaseNode = targetNode;

            RailNode foundTargetNode = (targetNode.neighbors.Count > 0)? targetNode.neighbors.First() : targetNode;
            currentTargetNode = foundTargetNode;
            
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
            if (currentCharacter != null)
                currentCharacter.Warp(position);

            if (startNode != null)
            {
                currentBaseNode = startNode;
                currentTargetNode = startNode.neighbors.Count > 0 ? startNode.neighbors[0] : startNode;
                RecalculatePathData();
            }

            TransitionToIdle();
        }

        private IEnumerator MoveAndSwitchRoutine(RailNode targetNode, CinemachineCamera newCam, float camDuration)
        {
            TransitionToIdle();
            
            _isFreeMoving = true;
            currentCharacter.MoveTo(targetNode.transform.position);

            float timeout = 10f;
            while(Vector3.Distance(currentCharacter.transform.position, targetNode.transform.position) >
                  WorldDefines.InteractionDistance && timeout>0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            currentBaseNode = targetNode;

            if(targetNode.neighbors.Count > 0)
                currentTargetNode = targetNode.neighbors[0];
            else
                currentTargetNode = targetNode;

            TransitionToIdle();
            RecalculatePathData();
            if(newCam != null)
                CameraManager.Instance.SwitchCamera(newCam, camDuration);
            _isFreeMoving = false;
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
                onEnter: () => { currentCharacter.MoveDir(Vector3.zero); },
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
            
            Vector3 camRight = CameraManager.Instance.GetCurrentCamera().transform.right;
            camRight.y = 0;
            Vector3 inputWorldDir = (camRight * inputX).normalized;

            if(currentBaseNode == currentTargetNode)
            {
                RailNode next = FindNeighborByDirection(inputWorldDir);
                if (next == null)
                    return;
             
                currentTargetNode = next;
                RecalculatePathData();
            }
            else 
            {
                float dot = Vector3.Dot(inputWorldDir, _cachedPathDir);
                if(dot < WorldDefines.DirectionReversalThreshold)
                {
                    (currentBaseNode, currentTargetNode) = (currentTargetNode, currentBaseNode);
                    RecalculatePathData();
                }
            }
            
            Vector3 currentPos = currentCharacter.Position;
            Vector3 basePos = currentBaseNode.transform.position;
            
            Vector3 toCharVector = currentPos - basePos;

            float t = Vector3.Dot(toCharVector, _cachedPathVector) / _cachedPathSqrLength;
            
            Vector3 projectedPos = basePos + (_cachedPathVector * t);
            projectedPos.y = currentPos.y; 

            Vector3 correction = projectedPos - currentPos;
        
            if(correction.sqrMagnitude > Mathf.Pow(WorldDefines.RailCorrectionDeadzone, 2))
                correction = Time.deltaTime * WorldDefines.DefaultCorrectionSpeed * correction.normalized;
            else 
                correction = Vector3.zero;

            currentCharacter.MoveDir(_cachedPathDir + correction);

            if(t >= 1.0f) 
            {
                currentBaseNode = currentTargetNode;
                _cachedPathVector = Vector3.zero; 
                _cachedPathSqrLength = 1f; 
                _cachedPathDir = Vector3.zero;
            }
        }

        private void RecalculatePathData()
        {
            _cachedPathVector = currentTargetNode.transform.position - currentBaseNode.transform.position;
            _cachedPathSqrLength = _cachedPathVector.sqrMagnitude;

            if(_cachedPathSqrLength < 0.001f)
                _cachedPathSqrLength = 1f;

            _cachedPathDir = _cachedPathVector.normalized;
        }

        private RailNode FindNeighborByDirection(Vector3 desiredDir)
        {
            RailNode bestNode = null;
            float maxDot = 0.1f;

            foreach(var neighbor in currentBaseNode.neighbors)
            {
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