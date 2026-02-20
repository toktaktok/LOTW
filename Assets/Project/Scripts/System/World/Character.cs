using System;
using Project.Scripts.Core.Managers;
using UnityEngine;
using UnityEngine.AI;
using Project.Scripts.Data;

namespace Project.Scripts.System.World
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Character : WorldObject
    {
        #region Properties

        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        
        [SerializeField] protected float moveSpeed = WorldDefines.DefaultMoveSpeed;
        private Vector3 _lastPosition;
        
        //actual visually rendered child's Transform
        private Transform _visualTransform;
        private Vector3 _initialScale;
        
        private NavMeshAgent _navMeshAgent;
        private static readonly int IsMoveHash = Animator.StringToHash("isMove");
        
        #endregion

        #region Methods

        public override void Init()
        {
            base.Init();

            _navMeshAgent = GetComponent<NavMeshAgent>();
            _navMeshAgent.updateRotation = false;
            _navMeshAgent.speed = moveSpeed;

            _lastPosition = transform.position;

            if(animator == null)
                animator = GetComponentInChildren<Animator>();
            
            if(spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _visualTransform = spriteRenderer.transform;
            _initialScale = _visualTransform.localScale;
        }

        public virtual void MoveTo(Vector3 destination)
        {
            if(_navMeshAgent.enabled)
            {
                _navMeshAgent.isStopped = false;
                _navMeshAgent.SetDestination(destination);
            }
        }
        public float MoveSpeed => moveSpeed;

        public virtual void MoveDir(Vector3 direction)
        {
            if(_navMeshAgent.enabled)
            {
                if(!_navMeshAgent.isStopped)
                    _navMeshAgent.ResetPath();

                _navMeshAgent.Move(Time.deltaTime * moveSpeed * direction);
            }
        }

        public void MoveOnRail(Vector3 railPosition)
        {
            if(!_navMeshAgent.enabled)
                return;

            if(!_navMeshAgent.isStopped)
                _navMeshAgent.ResetPath();

            _navMeshAgent.Warp(railPosition);
        }
        public void Warp(Vector3 destination)
        {
            if(_navMeshAgent != null)
                _navMeshAgent.Warp(destination);
            else
                transform.position = destination;
            
            _lastPosition = destination;
        }
        
        private void Update()
        {
            UpdateAnimationState();
        }

        private void UpdateAnimationState()
        {
            if(animator == null)
                return;
            
            Vector3 currentPosition = transform.position;
            Vector3 worldDirection = (currentPosition - _lastPosition);
            _lastPosition = currentPosition;

            animator.SetBool(IsMoveHash, worldDirection.sqrMagnitude > Mathf.Epsilon);

            Vector3 localDirection = CameraManager.Instance.GetCurrentCamera().transform.InverseTransformDirection(worldDirection);
            if(Mathf.Abs(localDirection.x) > Mathf.Epsilon)
            {
                Vector3 targetScale = _initialScale;
                targetScale.x = Mathf.Abs(_initialScale.x) * (localDirection.x > 0 ? 1 : -1);
                _visualTransform.localScale = targetScale;
            }
        }

        #endregion
    }
}