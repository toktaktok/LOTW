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
        [SerializeField] protected float moveSpeed = WorldDefines.DefaultMoveSpeed;

        #endregion

        #region Methods

        public override void Init()
        {
            base.Init();
            _animator = GetComponent<Animator>();
            _navMeshAgent = GetComponent<NavMeshAgent>();

            _navMeshAgent.updateRotation = false;
            _navMeshAgent.speed = moveSpeed;
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

                _navMeshAgent.Move(direction * moveSpeed * Time.deltaTime);

                if(direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
                }
            }
        }

        public void LookAt(Vector3 target)
        {
            Vector3 direction = (target - transform.position).normalized;
            direction.y = 0;
            if(direction != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction);
        }

        #endregion
    }
}