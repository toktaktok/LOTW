using NUnit.Framework;
using Project.Scripts.Core.Managers;

namespace Tests.EditMode
{
    public class FlagStoreTests
    {
        [Test]
        public void Has_ReturnsFalse_WhenUnset()
        {
            var store = new FlagStore();
            Assert.IsFalse(store.Has("intro_done"));
        }

        [Test]
        public void Get_ReturnsZero_WhenUnset()
        {
            var store = new FlagStore();
            Assert.AreEqual(0, store.Get("gold_gate"));
        }

        [Test]
        public void Set_Default_MarksFlagPresent()
        {
            var store = new FlagStore();
            store.Set("met_npc");
            Assert.IsTrue(store.Has("met_npc"));
            Assert.AreEqual(1, store.Get("met_npc"));
        }

        [Test]
        public void Set_WithValue_StoresValue()
        {
            var store = new FlagStore();
            store.Set("chapter", 3);
            Assert.AreEqual(3, store.Get("chapter"));
            Assert.IsTrue(store.Has("chapter"));
        }

        [Test]
        public void Set_Overwrites_ExistingValue()
        {
            var store = new FlagStore();
            store.Set("chapter", 1);
            store.Set("chapter", 5);
            Assert.AreEqual(5, store.Get("chapter"));
        }

        [Test]
        public void Set_Zero_RemovesFlag()
        {
            var store = new FlagStore();
            store.Set("door_open", 1);
            store.Set("door_open", 0);
            Assert.IsFalse(store.Has("door_open"));
            Assert.AreEqual(0, store.Get("door_open"));
            Assert.AreEqual(0, store.Count);
        }

        [Test]
        public void Set_NullOrEmptyKey_Ignored()
        {
            var store = new FlagStore();
            store.Set(null, 5);
            store.Set("", 5);
            Assert.AreEqual(0, store.Count);
        }

        [Test]
        public void Has_NullKey_ReturnsFalse()
        {
            var store = new FlagStore();
            Assert.IsFalse(store.Has(null));
            Assert.IsFalse(store.Has(""));
        }

        [Test]
        public void Add_AccumulatesValue()
        {
            var store = new FlagStore();
            Assert.AreEqual(1, store.Add("kills"));
            Assert.AreEqual(3, store.Add("kills", 2));
            Assert.AreEqual(3, store.Get("kills"));
        }

        [Test]
        public void Add_ToZero_RemovesFlag()
        {
            var store = new FlagStore();
            store.Set("counter", 2);
            store.Add("counter", -2);
            Assert.IsFalse(store.Has("counter"));
            Assert.AreEqual(0, store.Count);
        }

        [Test]
        public void Clear_RemovesSingleFlag()
        {
            var store = new FlagStore();
            store.Set("a");
            store.Set("b");
            store.Clear("a");
            Assert.IsFalse(store.Has("a"));
            Assert.IsTrue(store.Has("b"));
        }

        [Test]
        public void ClearAll_RemovesEverything()
        {
            var store = new FlagStore();
            store.Set("a");
            store.Set("b", 4);
            store.ClearAll();
            Assert.AreEqual(0, store.Count);
            Assert.IsFalse(store.Has("a"));
            Assert.IsFalse(store.Has("b"));
        }

        [Test]
        public void Count_ReflectsDistinctKeys()
        {
            var store = new FlagStore();
            store.Set("a");
            store.Set("b", 2);
            store.Set("a", 9); // overwrite, not a new key
            Assert.AreEqual(2, store.Count);
        }

        [Test]
        public void ToSaveData_EmitsKeyValuePairs()
        {
            var store = new FlagStore();
            store.Set("intro", 1);
            store.Set("chapter", 4);

            var data = store.ToSaveData();
            CollectionAssert.AreEquivalent(
                new[] { "intro=1", "chapter=4" }, data);
        }

        [Test]
        public void ToSaveData_Empty_ReturnsEmptyArray()
        {
            var store = new FlagStore();
            var data = store.ToSaveData();
            Assert.IsNotNull(data);
            Assert.AreEqual(0, data.Length);
        }

        [Test]
        public void LoadFromSaveData_RestoresState()
        {
            var store = new FlagStore();
            store.LoadFromSaveData(new[] { "intro=1", "chapter=4" });
            Assert.IsTrue(store.Has("intro"));
            Assert.AreEqual(4, store.Get("chapter"));
            Assert.AreEqual(2, store.Count);
        }

        [Test]
        public void LoadFromSaveData_Null_ClearsState()
        {
            var store = new FlagStore();
            store.Set("stale", 1);
            store.LoadFromSaveData(null);
            Assert.AreEqual(0, store.Count);
        }

        [Test]
        public void LoadFromSaveData_ReplacesExisting()
        {
            var store = new FlagStore();
            store.Set("old", 1);
            store.LoadFromSaveData(new[] { "fresh=2" });
            Assert.IsFalse(store.Has("old"));
            Assert.AreEqual(2, store.Get("fresh"));
        }

        [Test]
        public void LoadFromSaveData_BareKey_DefaultsToOne()
        {
            var store = new FlagStore();
            store.LoadFromSaveData(new[] { "lonely" });
            Assert.AreEqual(1, store.Get("lonely"));
        }

        [Test]
        public void LoadFromSaveData_SkipsMalformedAndZero()
        {
            var store = new FlagStore();
            store.LoadFromSaveData(new[] { "good=2", "bad=notanint", "", "zero=0", null });
            Assert.AreEqual(2, store.Get("good"));
            Assert.IsFalse(store.Has("bad"));
            Assert.IsFalse(store.Has("zero"));
            Assert.AreEqual(1, store.Count);
        }

        [Test]
        public void SaveLoad_RoundTrip_PreservesState()
        {
            var source = new FlagStore();
            source.Set("intro", 1);
            source.Set("chapter", 7);
            source.Set("score", 42);

            var restored = new FlagStore();
            restored.LoadFromSaveData(source.ToSaveData());

            Assert.AreEqual(source.Count, restored.Count);
            Assert.AreEqual(1, restored.Get("intro"));
            Assert.AreEqual(7, restored.Get("chapter"));
            Assert.AreEqual(42, restored.Get("score"));
        }
    }
}
