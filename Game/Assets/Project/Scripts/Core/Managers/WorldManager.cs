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

        #endregion

        #region Properties

        public IReadOnlyDictionary<int, WorldObject> All => _registry.All;

        #endregion

        #region Methods

        public void Register(WorldObject obj)
        {
            if(obj == null)
                return;
            _registry.Register(obj.ObjectID, obj);
        }

        public void Unregister(WorldObject obj)
        {
            if(obj == null)
                return;
            _registry.Unregister(obj.ObjectID);
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
