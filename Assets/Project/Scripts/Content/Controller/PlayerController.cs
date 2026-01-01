using UnityEngine;
using UnityEngine.InputSystem;
using Project.Scripts.System.World;
using Project.Scripts.Content.World;
using Project.Scripts.Data;

namespace Project.Scripts.Content.Controller
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Character currentCharacter;

        [Header("Rail System")]
        [SerializeField] private RailNode currentBaseNode;
        [SerializeField] private RailNode currentTargetNode;

        private Camera _mainCamera;
        
        private InputAction _moveAction;
        private void OnEnable() => _moveAction.Enable();
        private void OnDisable() => _moveAction.Disable();
        
        private void Awake()
        {
            _moveAction = new InputAction("Move");
        
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
            _mainCamera = Camera.main;
            
            if(currentCharacter != null)
                currentCharacter.Init();

            if(currentTargetNode == null)
                currentTargetNode = currentBaseNode;
        }

        private void Update()
        {
            if(currentCharacter == null || currentBaseNode == null) return;

            Vector2 input = _moveAction.ReadValue<Vector2>();

            if(input.sqrMagnitude > WorldDefines.InputThreshold)
            {
                MoveOnPath(input);
            }
            else
            {
                currentCharacter.MoveDirect(Vector3.zero);
            }
        }

        private void MoveOnPath(Vector2 input)
        {
            Vector3 camForward = _mainCamera.transform.forward;
            Vector3 camRight = _mainCamera.transform.right;

            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 inputWorldDir = (camRight*input.x + camForward*input.y).normalized;
            if(currentBaseNode == currentTargetNode)
            {
                RailNode next = FindNeighborByDirection(inputWorldDir);
                if(next == null)
                    return;
                currentTargetNode = next;
            }
            Vector3 pathVector = currentTargetNode.transform.position - currentBaseNode.transform.position;
            Vector3 pathDir = pathVector.normalized;
        
            float dot = Vector3.Dot(inputWorldDir, pathDir);
            if(dot < WorldDefines.DirectionReversalThreshold)
            {
                (currentBaseNode, currentTargetNode) = (currentTargetNode, currentBaseNode);
                pathDir = (currentTargetNode.transform.position - currentBaseNode.transform.position).normalized;
            }

            Vector3 currentPos = currentCharacter.Position;
            Vector3 basePos = currentBaseNode.transform.position;
            Vector3 targetPos = currentTargetNode.transform.position;

            Vector3 projectedPos = GetProjectedPointOnLine(basePos, targetPos, currentPos);
            projectedPos.y = currentPos.y;

            Vector3 correction = (projectedPos - currentPos);
        
            if(correction.magnitude > WorldDefines.RailCorrectionDeadzone) 
                correction = Time.deltaTime * WorldDefines.DefaultCorrectionSpeed * correction.normalized;
            else 
                correction = Vector3.zero;

            currentCharacter.MoveDirect(pathDir + correction.normalized);

            Vector3 toCharVector = currentCharacter.transform.position - currentBaseNode.transform.position;
            float t = Vector3.Dot(toCharVector, pathVector) / pathVector.sqrMagnitude;
            if (t >= 1.0f) 
                currentBaseNode = currentTargetNode;
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
        
        private Vector3 GetProjectedPointOnLine(Vector3 lineStart, Vector3 lineEnd, Vector3 targetPoint)
        {
            Vector3 toTarget = targetPoint - lineStart;
            Vector3 lineVector = lineEnd - lineStart;

            float dotProduct = Vector3.Dot(toTarget, lineVector);
            float projectionRatio = Mathf.Clamp01(dotProduct / lineVector.sqrMagnitude);

            return lineStart + (lineVector * projectionRatio);
        }
    }
}