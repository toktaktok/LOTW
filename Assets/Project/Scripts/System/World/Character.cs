using System;
using UnityEngine;
using UnityEngine.AI;
using Project.Scripts.Data;

namespace Project.Scripts.System.World
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class Character : WorldObject
    {
        #region Properties

        private NavMeshAgent _navMeshAgent;
        private Animator _animator;
        
        //actual visually rendered child's Transform
        private Transform _visualTransform;
        private Vector3 _initialScale;
        
        [SerializeField] protected float moveSpeed = WorldDefines.DefaultMoveSpeed;

        private Vector3 _lastPosition;
        private static readonly int IsMoveHash = Animator.StringToHash("isMove");
        
        #endregion

        #region Methods

        public override void Init()
        {
            base.Init();
            
            _animator = GetComponent<Animator>();
            
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _navMeshAgent.updateRotation = false;
            _navMeshAgent.speed = moveSpeed;

            _lastPosition = transform.position;
            
            SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                _visualTransform = spriteRenderer.transform;
                _initialScale = _visualTransform.localScale;
            }
            else
            {
                _visualTransform = transform;
                _initialScale = transform.localScale;
            }
        }

        public virtual void MoveTo(Vector3 destination)
        {
            if(_navMeshAgent.enabled)
            {
                _navMeshAgent.isStopped = false;
                _navMeshAgent.SetDestination(destination);
            }
        }

        public virtual void MoveDirect(Vector3 direction)
        {
            if(_navMeshAgent.enabled)
            {
                if(!_navMeshAgent.isStopped)
                    _navMeshAgent.ResetPath();

                _navMeshAgent.Move(Time.deltaTime * moveSpeed * direction);
            }
        }

        private void Update()
        {
            UpdateAnimationState();
        }

        private void UpdateAnimationState()
        {
            if(_animator == null)
                return;
            
            Vector3 currentPosition = transform.position;
            Vector3 direction = (currentPosition - _lastPosition);
            _lastPosition = currentPosition;
            _animator.SetBool(IsMoveHash, direction.sqrMagnitude > Mathf.Epsilon);

            if(Mathf.Abs(direction.x) > Mathf.Epsilon)
            {
                Vector3 targetScale = _initialScale;
                targetScale.x = Mathf.Abs(_initialScale.x) * (direction.x > 0 ? 1 : -1);
                _visualTransform.localScale = targetScale;
            }
        }

        #endregion
    }
}