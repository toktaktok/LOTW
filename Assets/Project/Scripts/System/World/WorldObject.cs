using UnityEngine;

namespace Project.Scripts.System.World
{
    public abstract class WorldObject : MonoBehaviour
    {
        #region Properties
        
        [SerializeField]
        private int objectID;
        
        public Vector3 Position => transform.position;
        
        #endregion
        
        #region Methods

        public virtual void Init()
        {
        }

        #endregion
    }
}
