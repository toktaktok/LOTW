using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Project.Scripts.Core.Managers;
using Project.Scripts.System.World;

namespace Tests.EditMode
{
    public class WorldManagerTests
    {
        private class TestWorldObject : WorldObject
        {
        }

        // EditMode에서는 Awake가 호출되지 않으므로 등록은 테스트에서 직접 호출
        private readonly List<GameObject> _created = new();
        private WorldManager _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = Create("WorldManager").AddComponent<WorldManager>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach(GameObject go in _created)
                Object.DestroyImmediate(go);
            _created.Clear();
        }

        private GameObject Create(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go;
        }

        private TestWorldObject CreateObject(string name, int id)
        {
            TestWorldObject obj = Create(name).AddComponent<TestWorldObject>();
            obj.SetObjectID(id);
            return obj;
        }

        [Test]
        public void Register_UnassignedIds_GetDistinctRuntimeIds()
        {
            TestWorldObject a = CreateObject("A", 0);
            TestWorldObject b = CreateObject("B", 0);

            _manager.Register(a);
            _manager.Register(b);

            Assert.Less(a.ObjectID, 0);
            Assert.Less(b.ObjectID, 0);
            Assert.AreNotEqual(a.ObjectID, b.ObjectID);
            Assert.AreSame(a, _manager.GetObject(a.ObjectID));
            Assert.AreSame(b, _manager.GetObject(b.ObjectID));
        }

        [Test]
        public void Register_DuplicateId_KeepsFirstAndReassignsSecond()
        {
            TestWorldObject first = CreateObject("First", 5);
            TestWorldObject second = CreateObject("Second", 5);
            LogAssert.Expect(LogType.Warning, new Regex("Duplicate objectID 5"));

            _manager.Register(first);
            _manager.Register(second);

            Assert.AreSame(first, _manager.GetObject(5));
            Assert.AreNotEqual(5, second.ObjectID);
            Assert.AreSame(second, _manager.GetObject(second.ObjectID));
        }

        [Test]
        public void Register_SameObjectTwice_KeepsId()
        {
            TestWorldObject obj = CreateObject("Obj", 3);

            _manager.Register(obj);
            _manager.Register(obj);

            Assert.AreEqual(3, obj.ObjectID);
            Assert.AreEqual(1, _manager.All.Count);
        }

        [Test]
        public void Unregister_OnlyRemovesOwnEntry()
        {
            TestWorldObject first = CreateObject("First", 5);
            TestWorldObject stranger = CreateObject("Stranger", 5);
            _manager.Register(first);

            // 등록되지 않은 같은 ID 객체가 파괴돼도 기존 등록을 지우지 않아야 함
            Assert.IsFalse(_manager.Unregister(stranger));
            Assert.AreSame(first, _manager.GetObject(5));

            Assert.IsTrue(_manager.Unregister(first));
            Assert.IsNull(_manager.GetObject(5));
        }

        [Test]
        public void ObjectRegistry_UnregisterWithObject_ChecksIdentity()
        {
            var registry = new ObjectRegistry<string>();
            registry.Register(1, "a");

            Assert.IsFalse(registry.Unregister(1, "b"));
            Assert.IsTrue(registry.Contains(1));
            Assert.IsTrue(registry.Unregister(1, "a"));
            Assert.IsFalse(registry.Contains(1));
        }
    }
}
