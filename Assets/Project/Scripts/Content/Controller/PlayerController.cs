using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

using Project.Scripts.Core;
using Project.Scripts.Data;
using Project.Scripts.System.World;
using Project.Scripts.Content.World;
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
        private InputAction _moveAction;

        private Vector3 _cachedPathVector;
        private float _cachedPathSqrLength;
        private Vector3 _cachedPathDir;

        private void OnEnable() => _moveAction.Enable();
        private void OnDisable() => _moveAction.Disable();

        private void Awake()
        {
            _moveAction = new InputAction("Move");
            _fsm = new StateMachine<PlayerController>(this);

            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            _moveAction.AddBinding("<Gamepad>/leftStick");
            _moveAction.AddBinding("<Gamepad>/dpad");
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

        private void OnIdle()
        {
            float inputX = _moveAction.ReadValue<Vector2>().x;
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
                    float inputX = _moveAction.ReadValue<Vector2>().x;

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
        
            if(correction.sqrMagnitude > WorldDefines.RailCorrectionDeadzone * WorldDefines.RailCorrectionDeadzone) 
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