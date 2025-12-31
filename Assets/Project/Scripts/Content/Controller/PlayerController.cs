using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Project.Scripts.System.Trigger;
using Project.Scripts.System.World;
using Project.Scripts.Content.World;

namespace Project.Scripts.Content.Controller
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Character currentCharacter;

        [Header("Rail System")] [SerializeField]
        private RailNode currentBaseNode;

        [SerializeField] private RailNode currentTargetNode;

        [SerializeField] private float correctionSpeed = 10f;
        private Camera _mainCam;

        private InputAction _moveAction;
        
        private void Awake()
        {
            _moveAction = new InputAction("Move");
        
            _moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
        
            _moveAction.AddBinding("<Gamepad>/leftStick/x");
            _moveAction.AddBinding("<Gamepad>/dpad/x");
        }
        
        private void OnEnable()
        {
            _moveAction.Enable();
        }

        private void OnDisable()
        {
            _moveAction.Disable();
        }
        
        private void Start()
        {
            _mainCam = Camera.main;
            if(currentCharacter != null) currentCharacter.Init();

            if(currentTargetNode == null) currentTargetNode = currentBaseNode;
        }

        private void Update()
        {
            if(currentCharacter == null || currentBaseNode == null) return;

            float h = _moveAction.ReadValue<float>();
            
            if(Mathf.Abs(h) > 0.01f)
            {
                MoveOnPath(h);
            }
            else
            {
                currentCharacter.MoveDirect(Vector3.zero);
            }
        }

        private void MoveOnPath(float input)
        {
            if(currentBaseNode == currentTargetNode)
            {
                RailNode next = FindNeighborByInput(input);
                if(next != null)
                {
                    currentTargetNode = next;
                }
                else
                {
                    return;
                }
            }

            Vector3 pathDir = (currentTargetNode.transform.position - currentBaseNode.transform.position).normalized;

            Vector3 inputWorldDir = _mainCam.transform.right * input;
            float dot = Vector3.Dot(inputWorldDir, pathDir);

            if(dot < -0.1f)
            {
                (currentBaseNode, currentTargetNode) = (currentTargetNode, currentBaseNode);
                pathDir = (currentTargetNode.transform.position - currentBaseNode.transform.position).normalized;
            }

            Vector3 currentPos = currentCharacter.Position;
            Vector3 a = currentBaseNode.transform.position;
            Vector3 b = currentTargetNode.transform.position;

            Vector3 projectedPos = GetProjectedPointOnLine(a, b, currentPos);
            projectedPos.y = currentPos.y;

            Vector3 correction = (projectedPos - currentPos);
            if(correction.magnitude > 0.05f)
            {
                correction = correction.normalized * correctionSpeed * Time.deltaTime;
            }
            else
            {
                correction = Vector3.zero;
            }

            currentCharacter.MoveDirect(pathDir + correction.normalized);

            float dist = Vector3.Distance(currentPos, b);
            if(dist < 0.2f)
            {
                currentBaseNode = currentTargetNode;
            }
        }

        private RailNode FindNeighborByInput(float input)
        {
            Vector3 desiredDir = _mainCam.transform.right * input;

            RailNode bestNode = null;
            float maxDot = 0f;

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

        private Vector3 GetProjectedPointOnLine(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ap = p - a;
            Vector3 ab = b - a;
            float t = Vector3.Dot(ap, ab) / ab.sqrMagnitude;
            t = Mathf.Clamp01(t);
            return a + ab * t;
        }
    }
}