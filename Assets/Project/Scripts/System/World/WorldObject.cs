using UnityEngine;
using Project.Scripts.Core.Managers;

namespace Project.Scripts.System.World
{
    public abstract class WorldObject : MonoBehaviour
    {
        #region Properties

        [SerializeField] private int objectID;

        public int ObjectID => objectID;
        public Vector3 Position => transform.position;

        #endregion

        #region Methods

        protected virtual void Awake()
        {
            WorldManager.Instance.Register(this);
        }

        protected virtual void OnDestroy()
        {
            if (WorldManager.Instance != null)
                WorldManager.Instance.Unregister(this);
        }

        public virtual void Init()
        {
        }

        #endregion
    }
}