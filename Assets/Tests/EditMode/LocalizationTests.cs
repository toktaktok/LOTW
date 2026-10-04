using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Project.Scripts.Core;
using Project.Scripts.Data.Table;

namespace Tests.EditMode
{
    public class LocalizationTests
    {
        private Dictionary<string, TextData> _table;

        [SetUp]
        public void SetUp()
        {
            _table = Localization.BuildTable(new[]
            {
                new TextData { dataId = 1, key = "ui.talk", ko = "대화하기", en = "Talk" },
                new TextData { dataId = 2, key = "ui.only_ko", ko = "한국어만", en = "" },
            });
        }

        [Test]
        public void Resolve_Key_ReturnsLanguageText()
        {
            Assert.AreEqual("대화하기", Localization.Resolve("@ui.talk", _table, "ko"));
            Assert.AreEqual("Talk", Localization.Resolve("@ui.talk", _table, "en"));
        }

        [Test]
        public void Resolve_UnknownLanguage_FallsBackToKo()
        {
            Assert.AreEqual("대화하기", Localization.Resolve("@ui.talk", _table, "ja"));
            Assert.AreEqual("대화하기", Localization.Resolve("@ui.talk", _table, null));
        }

        [Test]
        public void Resolve_EmptyTranslation_FallsBackToKo()
        {
            Assert.AreEqual("한국어만", Localization.Resolve("@ui.only_ko", _table, "en"));
        }

        [Test]
        public void Resolve_Literal_ReturnedAsIs()
        {
            Assert.AreEqual("대화하기", Localization.Resolve("대화하기", _table, "en"));
            Assert.IsNull(Localization.Resolve(null, _table, "en"));
            Assert.AreEqual("", Localization.Resolve("", _table, "en"));
        }

        [Test]
        public void Resolve_MissingKey_ShowsKey()
        {
            Assert.AreEqual("@ui.missing", Localization.Resolve("@ui.missing", _table, "ko"));
        }

        [Test]
        public void Resolve_NoTable_ShowsKey()
        {
            Assert.AreEqual("@ui.talk", Localization.Resolve("@ui.talk", null, "ko"));
        }

        [Test]
        public void BuildTable_DuplicateKey_LaterWinsWithWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Duplicate text key 'k'"));

            Dictionary<string, TextData> table = Localization.BuildTable(new[]
            {
                new TextData { dataId = 1, key = "k", ko = "first" },
                new TextData { dataId = 2, key = "k", ko = "second" },
                new TextData { dataId = 3, key = "", ko = "ignored" },
            });

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("second", table["k"].ko);
        }
    }
}
