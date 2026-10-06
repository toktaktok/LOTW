using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using UnityEngine;
using UnityEngine.TestTools;
using Project.Scripts.Framework.Managers;

namespace Tests.EditMode
{
    public class SaveSystemTests
    {
        private string _root;
        private SaveSystem<SaveData> _system;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "lotw_save_tests_" + Path.GetRandomFileName());
            _system = new SaveSystem<SaveData>(maxSlots: 2, rootPath: _root);
        }

        [TearDown]
        public void TearDown()
        {
            if(Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [Test]
        public void SaveThenLoad_RoundTrips_AndOverwriteLeavesNoTemp()
        {
            Assert.IsTrue(_system.Save(0, new SaveData { version = 1, playTime = 12.5f, flags = new[] { "quest.1=1" } }));
            Assert.IsTrue(_system.Save(0, new SaveData { version = 1, playTime = 20f, flags = new[] { "quest.1=2" } }));

            SaveData? loaded = _system.Load(0);

            Assert.IsTrue(loaded.HasValue);
            Assert.AreEqual(20f, loaded.Value.playTime);
            CollectionAssert.AreEqual(new[] { "quest.1=2" }, loaded.Value.flags);
            Assert.AreEqual(1, Directory.GetFiles(Path.Combine(_root, "Saves")).Length);
        }

        [Test]
        public void MissingOrInvalidSlot_ReturnsNull()
        {
            Assert.IsFalse(_system.Load(1).HasValue);
            Assert.IsFalse(_system.Load(5).HasValue);
            Assert.IsFalse(_system.Save(-1, new SaveData()));
            Assert.IsFalse(_system.HasSave(1));
        }

        [Test]
        public void EmptyFile_ReturnsNullWithError()
        {
            Directory.CreateDirectory(Path.Combine(_root, "Saves"));
            File.WriteAllText(Path.Combine(_root, "Saves", "save_0.json"), "");
            LogAssert.Expect(LogType.Error, new Regex("Failed to load slot 0"));

            Assert.IsFalse(_system.Load(0).HasValue);
        }

        [Test]
        public void Upgrade_OldVersion_SetsCurrent()
        {
            SaveData upgraded = SaveManager.Upgrade(new SaveData { version = 0 });

            Assert.AreEqual(SaveDefines.Version, upgraded.version);
        }
    }
}
