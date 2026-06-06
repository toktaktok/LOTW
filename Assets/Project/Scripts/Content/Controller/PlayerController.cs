using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

using Project.Scripts.Core;
using Project.Scripts.Data;
using Project.Scripts.System.World;
using Project.Scripts.Content.World;
using Project.Scripts.Content.UI;
using Project.Scripts.Core.Managers;

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

            UIManager.Instance.PushPage<HudUI>(UILayer.HUD);
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

            RailNode foundTargetNode = (targetNode.neighbors.Count > 0) ? targetNode.neighbors[0] : targetNode;
            currentTargetNode = foundTargetNode;
            
            RecalculatePathData();
            TransitionToIdle();
        }
        public void MoveToAndSwitchPath(RailNode targetNode, CinemachineCamera newCam, float camDuration)
        {
            StartCoroutine(MoveAndSwitchRoutine(targetNode, newCam, camDuration));
        }

        private IEnumerator MoveAndSwitchRoutine(RailNode targetNode, CinemachineCamera newCam, float camDuration)
        {
            TransitionToIdle();
            
            _isFreeMoving = true;
            currentCharacter.MoveTo(targetNode.transform.position);

            float timeout = WorldDefines.MoveAndSwitchTimeout;
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