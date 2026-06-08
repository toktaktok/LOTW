using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.System.World;

namespace Project.Scripts.Core.Managers
{
    public class WorldManager : Singleton<WorldManager>
    {
        #region Fields

        private readonly Dictionary<int, WorldObject> _worldObjects = new();

        #endregion

        #region Methods

        public void Register(WorldObject obj)
        {
            if (obj == null) return;
            _worldObjects[obj.ObjectID] = obj;
        }

        public void Unregister(WorldObject obj)
        {
            if (obj == null) return;
            _worldObjects.Remove(obj.ObjectID);
        }

        public WorldObject GetObject(int objectID)
        {
            _worldObjects.TryGetValue(objectID, out WorldObject obj);
            return obj;
        }

        public T GetObject<T>(int objectID) where T : WorldObject
        {
            return GetObject(objectID) as T;
        }

        #endregion
    }
}
