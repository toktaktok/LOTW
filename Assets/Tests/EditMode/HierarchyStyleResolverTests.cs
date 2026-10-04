using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Project.Scripts.Editor.Data;
using Project.Scripts.Editor.Enhancers;

namespace Tests.EditMode
{
    public class HierarchyStyleResolverTests
    {
        [TestCase("Enemy", "Enemy", true)]
        [TestCase("enemy", "ENEMY", true)]
        [TestCase("Enemy (1)", "Enemy", false)]
        [TestCase("Enemy (1)", "Enemy*", true)]
        [TestCase("UI_Hud", "UI_*", true)]
        [TestCase("Hud_UI", "*_UI", true)]
        [TestCase("Main Camera", "*Camera*", true)]
        [TestCase("Camera", "*Camera*", true)]
        [TestCase("Cam", "*Camera*", false)]
        [TestCase("Spawn_A_Point", "Spawn*Point", true)]
        [TestCase("SpawnPoint", "Spawn*Point", true)]
        [TestCase("Spawn", "Spawn*Spawn", false)]
        [TestCase("A_B_C", "A*B*C", true)]
        [TestCase("A_C_B", "A*B*C", false)]
        [TestCase("Anything", "*", true)]
        public void IsMatch_Wildcard(string name, string pattern, bool expected)
        {
            Assert.AreEqual(expected, HierarchyStyleResolver.IsMatch(name, pattern));
        }

        [Test]
        public void TryMatchRule_ReturnsFirstMatch_AndSkipsEmptyPattern()
        {
            var rules = new List<FolderRule>
            {
                new FolderRule { name = "", color = Color.white },
                new FolderRule { name = "Enemy*", color = Color.red, icon = "Prefab Icon" },
                new FolderRule { name = "*", color = Color.blue },
            };

            Assert.IsTrue(HierarchyStyleResolver.TryMatchRule("Enemy (2)", rules, out FolderRule match));
            Assert.AreEqual(Color.red, match.color);
            Assert.AreEqual("Prefab Icon", match.icon);

            Assert.IsTrue(HierarchyStyleResolver.TryMatchRule("Player", rules, out match));
            Assert.AreEqual(Color.blue, match.color);
        }

        [Test]
        public void TryMatchRule_NoMatch_ReturnsFalse()
        {
            var rules = new List<FolderRule> { new FolderRule { name = "UI_*", color = Color.red } };

            Assert.IsFalse(HierarchyStyleResolver.TryMatchRule("Player", rules, out _));
        }
    }
}
