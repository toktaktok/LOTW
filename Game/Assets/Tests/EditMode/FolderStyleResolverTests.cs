using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Project.Scripts.Editor.Data;
using Project.Scripts.Editor.Enhancers;

namespace Tests.EditMode
{
    public class FolderStyleResolverTests
    {
        private Dictionary<string, FolderStyle> _styles;
        private FolderStyleResolver.StyleLookup _lookup;

        [SetUp]
        public void SetUp()
        {
            _styles = new Dictionary<string, FolderStyle>();
            _lookup = (string path, out FolderStyle style) => _styles.TryGetValue(path, out style);
        }

        [Test]
        public void DirectStyle_WinsOverRuleAndParent()
        {
            _styles["Assets/Art"] = new FolderStyle { color = Color.red, inherit = true };
            _styles["Assets/Art/Scripts"] = new FolderStyle { color = Color.green };
            var rules = new List<FolderRule> { new FolderRule { name = "Scripts", color = Color.blue } };

            Assert.IsTrue(FolderStyleResolver.TryResolve("Assets/Art/Scripts", _lookup, rules, out FolderStyle result));
            Assert.AreEqual(Color.green, result.color);
        }

        [Test]
        public void Rule_MatchesNameCaseInsensitive_AndWinsOverParent()
        {
            _styles["Assets/Art"] = new FolderStyle { color = Color.red, inherit = true };
            var rules = new List<FolderRule> { new FolderRule { name = "scripts", color = Color.blue, icon = "cs Script Icon" } };

            Assert.IsTrue(FolderStyleResolver.TryResolve("Assets/Art/Scripts", _lookup, rules, out FolderStyle result));
            Assert.AreEqual(Color.blue, result.color);
            Assert.AreEqual("cs Script Icon", result.icon);
        }

        [Test]
        public void InheritedParent_AppliesToDeepDescendants()
        {
            _styles["Assets/Art"] = new FolderStyle { color = Color.red, inherit = true };

            Assert.IsTrue(FolderStyleResolver.TryResolve("Assets/Art/Characters/Hero", _lookup, null, out FolderStyle result));
            Assert.AreEqual(Color.red, result.color);
        }

        [Test]
        public void NonInheritingParent_DoesNotApply()
        {
            _styles["Assets/Art"] = new FolderStyle { color = Color.red, inherit = false };

            Assert.IsFalse(FolderStyleResolver.TryResolve("Assets/Art/Characters", _lookup, null, out _));
        }

        [Test]
        public void NearestInheritingAncestor_Wins()
        {
            _styles["Assets"] = new FolderStyle { color = Color.red, inherit = true };
            _styles["Assets/Art"] = new FolderStyle { color = Color.green, inherit = true };

            Assert.IsTrue(FolderStyleResolver.TryResolve("Assets/Art/Characters", _lookup, null, out FolderStyle result));
            Assert.AreEqual(Color.green, result.color);
        }

        [Test]
        public void NullRules_SkipsRuleMatching()
        {
            Assert.IsFalse(FolderStyleResolver.TryResolve("Assets/Scripts", _lookup, null, out _));
        }

        [Test]
        public void EmptyPath_ReturnsFalse()
        {
            Assert.IsFalse(FolderStyleResolver.TryResolve(string.Empty, _lookup, null, out _));
        }
    }
}
