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

        /// <summary>맵 빌더가 할당자(MapModel.nextId)로 발급한 고유 ID를 스폰 시 주입합니다.</summary>
        public void SetObjectID(int id) => objectID = id;

        #endregion

        #region Methods

        protected virtual void Awake()
        {
            WorldManager.Instance.Register(this);
        }

        protected virtual void OnDestroy()
        {
            if(WorldManager.HasInstance)
                WorldManager.Instance.Unregister(this);
        }

        public virtual void Init()
        {
        }

        #endregion
    }
}