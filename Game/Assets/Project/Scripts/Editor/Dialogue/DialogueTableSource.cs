#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Project.Scripts.Core;
using Project.Scripts.Data.Table;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor.Dialogue
{
    /// <summary>
    /// 에디터에서 편집 중인 Dialogue 행과 Text_Dialogue 행. Resources/Table 의 JSON 에서 읽습니다.
    /// ScriptableObject 라서 Undo.RecordObject 로 편집을 되돌릴 수 있습니다 (DialogueEdits).
    /// 읽어 온 시점의 행을 JSON 으로 보관해 두고, 저장할 때 바뀐 행만 고릅니다.
    /// 다른 Text_* 파일은 대사 표시에만 쓰고 편집하지 않습니다.
    /// </summary>
    public class DialogueTableSource : ScriptableObject
    {
        [SerializeField] private List<DialogueData> lines = new();
        [SerializeField] private List<TextData> dialogueTexts = new();
        [SerializeField] private List<TextData> otherTexts = new();
        [SerializeField] private List<string> savedLines = new();
        [SerializeField] private List<string> savedTexts = new();

        private Dictionary<string, TextData> _textTable;

        public List<DialogueData> Lines => lines;
        public List<TextData> DialogueTexts => dialogueTexts;
        public IReadOnlyList<string> SavedLines => savedLines;
        public IReadOnlyList<string> SavedTexts => savedTexts;

        public bool IsDirty
        {
            get
            {
                return !savedLines.SequenceEqual(lines.Select(l => JsonUtility.ToJson(l)))
                    || !savedTexts.SequenceEqual(dialogueTexts.Select(t => JsonUtility.ToJson(t)));
            }
        }

        public static DialogueTableSource Load()
        {
            var lines = new List<DialogueData>();
            var dialogueTexts = new List<TextData>();
            var otherTexts = new List<TextData>();
            foreach(TextAsset asset in Resources.LoadAll<TextAsset>(ToolDefines.DialogueTableFolder))
            {
                if(asset.name == ToolDefines.DialogueTableName)
                    lines.AddRange(JsonArrayHelper.FromJson<DialogueData>(asset.text));
                else if(asset.name == ToolDefines.DialogueTextTable)
                    dialogueTexts.AddRange(JsonArrayHelper.FromJson<TextData>(asset.text));
                else if(asset.name.StartsWith(ToolDefines.TextTablePrefix))
                    otherTexts.AddRange(JsonArrayHelper.FromJson<TextData>(asset.text));
            }
            return Create(lines, dialogueTexts, otherTexts);
        }

        /// <summary>주어진 행을 저장된 상태로 보고 만듭니다 (Load, 테스트).</summary>
        public static DialogueTableSource Create(IEnumerable<DialogueData> lines, IEnumerable<TextData> dialogueTexts, IEnumerable<TextData> otherTexts)
        {
            var source = CreateInstance<DialogueTableSource>();
            source.hideFlags = HideFlags.HideAndDontSave;
            source.lines.AddRange(lines.OrderBy(l => l.dataId));
            source.dialogueTexts.AddRange(dialogueTexts);
            source.otherTexts.AddRange(otherTexts);
            source.savedLines.AddRange(source.lines.Select(l => JsonUtility.ToJson(l)));
            source.savedTexts.AddRange(source.dialogueTexts.Select(t => JsonUtility.ToJson(t)));
            return source;
        }

        /// <summary>편집이나 Undo 뒤에 호출해 대사 조회 캐시를 다시 만들게 합니다.</summary>
        public void MarkChanged()
        {
            _textTable = null;
        }

        public DialogueData GetLine(int dataId) => lines.Find(l => l.dataId == dataId);

        /// <summary>Text_Dialogue 에 있는 키의 행. 없으면 null (다른 Text 파일의 키 포함).</summary>
        public TextData GetDialogueText(string key) => dialogueTexts.Find(t => t.key == key);

        /// <summary>'@키' 를 기본 언어(ko) 텍스트로. 키가 없으면 '@키' 그대로.</summary>
        public string Resolve(string text)
        {
            _textTable ??= Localization.BuildTable(otherTexts.Concat(dialogueTexts));
            return Localization.Resolve(text, _textTable, null);
        }

        /// <summary>이 '@키' 를 Text 로 쓰는 행 수.</summary>
        public int CountTextUses(string text) => lines.Count(l => l.text == text);

        public static int GetBlock(int dataId) => dataId / ToolDefines.DialogueBlockSize;

        public IEnumerable<int> GetBlocks() => lines.Select(l => GetBlock(l.dataId)).Distinct().OrderBy(b => b);

        public IEnumerable<DialogueData> GetBlockLines(int block)
        {
            return lines.Where(l => GetBlock(l.dataId) == block).OrderBy(l => l.dataId);
        }

        /// <summary>블록 안에서 다른 행이 가리키지 않는 행 (대화 시작 후보). 없으면 가장 작은 DataId.</summary>
        public List<DialogueData> GetBlockRoots(int block)
        {
            List<DialogueData> blockLines = GetBlockLines(block).ToList();
            var referenced = new HashSet<int>();
            foreach(DialogueData line in blockLines)
            {
                referenced.Add(line.nextId);
                if(line.choiceIds != null)
                    referenced.UnionWith(line.choiceIds);
            }

            List<DialogueData> roots = blockLines.Where(l => !referenced.Contains(l.dataId)).ToList();
            if(roots.Count == 0 && blockLines.Count > 0)
                roots.Add(blockLines[0]);
            return roots;
        }
    }
}
#endif
