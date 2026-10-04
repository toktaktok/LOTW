using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.System.World;

namespace Project.Scripts.Core.Managers
{
    /// <summary>
    /// ID 기반 오브젝트를 등록/조회하는 범용 레지스트리.
    /// MonoBehaviour가 아니므로 어디서든 독립적으로 사용 가능합니다.
    /// </summary>
    public class ObjectRegistry<T> where T : class
    {
        private readonly Dictionary<int, T> _objects = new();

        public IReadOnlyDictionary<int, T> All => _objects;
        public int Count => _objects.Count;

        public void Register(int id, T obj)
        {
            if(obj == null)
                return;
            _objects[id] = obj;
        }

        public void Unregister(int id)
        {
            _objects.Remove(id);
        }

        /// <summary>id에 등록된 객체가 obj일 때만 해제합니다. 같은 id를 다른 객체가 쓰고 있으면 건드리지 않습니다.</summary>
        public bool Unregister(int id, T obj)
        {
            if(!_objects.TryGetValue(id, out T current) || !ReferenceEquals(current, obj))
                return false;
            return _objects.Remove(id);
        }

        public bool Contains(int id) => _objects.ContainsKey(id);

        public T Get(int id)
        {
            _objects.TryGetValue(id, out T obj);
            return obj;
        }

        public TDerived Get<TDerived>(int id) where TDerived : class, T
        {
            return Get(id) as TDerived;
        }

        public void Clear()
        {
            _objects.Clear();
        }
    }

    /// <summary>
    /// WorldObject 전용 매니저. ObjectRegistry를 내부에서 사용합니다.
    /// </summary>
    public class WorldManager : Singleton<WorldManager>
    {
        #region Fields

        private readonly ObjectRegistry<WorldObject> _registry = new();

        // 맵 할당자(MapModel.nextId)는 1부터 양수를 쓰므로 런타임 자동 발급은 음수로 분리
        private int _lastRuntimeId;

        #endregion

        #region Properties

        public IReadOnlyDictionary<int, WorldObject> All => _registry.All;

        #endregion

        #region Methods

        /// <summary>
        /// objectID 0(미할당)이거나 다른 객체가 이미 쓰는 ID면 음수 런타임 ID를 발급해 등록합니다.
        /// </summary>
        public void Register(WorldObject obj)
        {
            if(obj == null)
                return;

            int id = obj.ObjectID;
            WorldObject current = _registry.Get(id);
            if(current == obj)
                return;

            if(id == 0 || current != null)
            {
                if(current != null && id != 0)
                    Debug.LogWarning($"[WorldManager] Duplicate objectID {id}: '{current.name}' and '{obj.name}'. Assigning a runtime ID to '{obj.name}'.");
                id = --_lastRuntimeId;
                obj.AssignRuntimeID(id);
            }

            _registry.Register(id, obj);
        }

        public bool Unregister(WorldObject obj)
        {
            if(obj == null)
                return false;
            return _registry.Unregister(obj.ObjectID, obj);
        }

        public WorldObject GetObject(int objectID)
        {
            return _registry.Get(objectID);
        }

        public T GetObject<T>(int objectID) where T : WorldObject
        {
            return _registry.Get<T>(objectID);
        }

        #endregion
    }
}
