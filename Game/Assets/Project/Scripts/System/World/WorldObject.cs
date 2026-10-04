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

        /// <summary>
        /// 맵 빌더가 할당자(MapModel.nextId)로 발급한 고유 ID를 스폰 시 주입합니다.
        /// Awake에서 이미 등록됐으므로 새 ID로 다시 등록합니다.
        /// </summary>
        public void SetObjectID(int id)
        {
            if(id == objectID)
                return;

            bool registered = WorldManager.HasInstance && WorldManager.Instance.Unregister(this);
            objectID = id;
            if(registered)
                WorldManager.Instance.Register(this);
        }

        /// <summary>WorldManager가 미할당/중복 ID 대신 발급한 런타임 ID를 기록합니다.</summary>
        internal void AssignRuntimeID(int id) => objectID = id;

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