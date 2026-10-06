#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;
using Project.Scripts.Editor.Data;
using Project.Scripts.System.Dialogue;
using Project.Scripts.Framework;
using Object = UnityEngine.Object;

namespace Project.Scripts.Editor.Dialogue
{
    /// <summary>
    /// 조건/액션 키 선택 목록. 아이템은 Item 테이블, 의뢰/수첩/시퀀스는 각 테이블의 DataId, 사운드는 AudioLibrary 클립 이름,
    /// 미니게임은 MinigameLibrary 의 ID, 플래그와 캐릭터(met, meet)는 모든 대화에서 쓰인 값입니다.
    /// 창을 열거나 새로고침할 때 만듭니다 (플래그와 캐릭터는 편집을 따라 매번 모음).
    /// </summary>
    public class CommandPickers
    {
        private readonly DialogueTableSource _source;
        private readonly string[] _items;
        private readonly string[] _quests;
        private readonly string[] _notes;
        private readonly string[] _sequences;
        private readonly string[] _sounds;
        private readonly string[] _minigames;

        public CommandPickers(DialogueTableSource source)
        {
            _source = source;
            var itemTable = Resources.Load<TextAsset>($"{ToolDefines.DialogueTableFolder}/{ToolDefines.DialogueItemTableName}");
            _items = itemTable != null ? JsonArrayHelper.FromJson<ItemTableData>(itemTable.text).Select(i => i.itemId).OrderBy(i => i).ToArray() : Array.Empty<string>();
            _quests = LoadIds<QuestData>("Quest");
            _notes = LoadIds<NotebookData>("Notebook");
            _sequences = LoadIds<SequenceData>("Sequence");
            _sounds = LoadNames(nameof(AudioLibrary), "clips", o => o.name);
            _minigames = LoadNames(nameof(MinigameLibrary), "definitions", o => ((MinigameDefinition)o).Id);
        }

        public string[] GetKeys(string verb)
        {
            switch(verb.ToLowerInvariant())
            {
                case "item":
                case "giveitem":
                case "takeitem":
                    return _items;
                case "sfx":
                case "bgm":
                    return _sounds;
                case "minigame":
                    return _minigames;
                case "quest":
                case "startquest":
                case "completequest":
                    return _quests;
                case "note":
                case "addnote":
                case "strikenote":
                    return _notes;
                case "sequence":
                    return _sequences;
                case "met":
                case "meet":
                    return _source.Lines.Select(l => l.speakerId).Where(s => !string.IsNullOrEmpty(s) && s != ToolDefines.DialoguePlayerSpeakerId).Distinct().OrderBy(s => s).ToArray();
                case "flag":
                case "setflag":
                case "addflag":
                case "clearflag":
                    return GetFlags();
                default:
                    return Array.Empty<string>();
            }
        }

        private string[] GetFlags()
        {
            return _source.Lines
                .SelectMany(l => DialogueCommands.Parse(l.conditions).Concat(DialogueCommands.Parse(l.actions)))
                .Where(c => c.verb.ToLowerInvariant() is "flag" or "setflag" or "addflag" or "clearflag" && c.key.Length > 0)
                .Select(c => c.key).Distinct().OrderBy(k => k).ToArray();
        }

        private static string[] LoadIds<T>(string table) where T : TableRowData
        {
            var asset = Resources.Load<TextAsset>($"{ToolDefines.DialogueTableFolder}/{table}");
            return asset != null ? JsonArrayHelper.FromJson<T>(asset.text).Select(r => r.dataId.ToString()).ToArray() : Array.Empty<string>();
        }

        /// <summary>type 에셋들의 arrayField 목록에 든 오브젝트 이름.</summary>
        private static string[] LoadNames(string type, string arrayField, Func<Object, string> getName)
        {
            var names = new HashSet<string>();
            foreach(string guid in AssetDatabase.FindAssets($"t:{type}"))
            {
                SerializedProperty array = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid))).FindProperty(arrayField);
                for(int i = 0; i < array.arraySize; i++)
                {
                    Object element = array.GetArrayElementAtIndex(i).objectReferenceValue;
                    if(element != null)
                        names.Add(getName(element));
                }
            }
            return names.OrderBy(n => n).ToArray();
        }
    }
}
#endif
