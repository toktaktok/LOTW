using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;
using UnityEngine;
using Project.Scripts.Framework;

namespace Tests.EditMode
{
    /// <summary>Sequence 테이블 스텝 형식과, 스텝/대화가 가리키는 ID 가 서로 맞는지 검사.</summary>
    public class SequenceTableTests
    {
        private static readonly Regex SequenceRef = new(@"sequence:(\d+)", RegexOptions.IgnoreCase);

        private List<SequenceData> _steps;
        private List<DialogueData> _dialogue;

        private static List<T> Load<T>(string table)
        {
            string path = Path.Combine(Application.dataPath, $"Project/Resources/Table/{table}.json");
            return JsonArrayHelper.FromJson<T>(File.ReadAllText(path));
        }

        [OneTimeSetUp]
        public void LoadTables()
        {
            _steps = Load<SequenceData>("Sequence");
            _dialogue = Load<DialogueData>("Dialogue");
        }

        [TestCase("dialogue", true)]
        [TestCase("fadeOut", true)]
        [TestCase("FADEIN", true)]
        [TestCase("3", true)]
        [TestCase("99", false)]
        [TestCase("jump", false)]
        [TestCase("", false)]
        public void TryParseType(string value, bool expected)
        {
            Assert.AreEqual(expected, SequenceData.TryParseType(value, out _));
        }

        [Test]
        public void Steps_HaveValidType_AndParams()
        {
            var dialogueIds = new HashSet<int>(_dialogue.Select(d => d.dataId));
            foreach(SequenceData step in _steps)
            {
                Assert.IsTrue(step.TryGetStepType(out SequenceStepType type), $"step {step.dataId}: type '{step.type}'");
                switch(type)
                {
                    case SequenceStepType.Dialogue:
                        Assert.IsTrue(int.TryParse(step.param, out int id) && dialogueIds.Contains(id), $"step {step.dataId}: dialogue '{step.param}'");
                        break;
                    case SequenceStepType.Scene:
                    case SequenceStepType.Camera:
                    case SequenceStepType.Actions:
                        Assert.IsFalse(string.IsNullOrEmpty(step.param), $"step {step.dataId}: empty param");
                        break;
                    case SequenceStepType.Wait:
                        Assert.Greater(step.duration, 0f, $"step {step.dataId}: wait needs duration");
                        break;
                }
            }
        }

        [Test]
        public void NextIds_Resolve_AndIdsUnique()
        {
            var ids = new HashSet<int>();
            foreach(SequenceData step in _steps)
                Assert.IsTrue(ids.Add(step.dataId), $"duplicate step {step.dataId}");

            foreach(SequenceData step in _steps)
                Assert.IsTrue(step.nextId == -1 || ids.Contains(step.nextId), $"step {step.dataId}: nextId {step.nextId}");

            Assert.IsTrue(ids.Contains(StoryDefines.PrologueSequenceId), "prologue start step missing");
        }

        [Test]
        public void DialogueSequenceActions_Resolve()
        {
            var ids = new HashSet<int>(_steps.Select(s => s.dataId));
            foreach(DialogueData line in _dialogue)
            {
                foreach(Match match in SequenceRef.Matches(line.actions ?? string.Empty))
                    Assert.IsTrue(ids.Contains(int.Parse(match.Groups[1].Value)), $"dialogue {line.dataId}: {match.Value}");
            }
        }
    }
}
