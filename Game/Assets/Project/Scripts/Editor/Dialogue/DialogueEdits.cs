#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Project.Scripts.Data.Table;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor.Dialogue
{
    /// <summary>
    /// DialogueTableSource 편집. 모든 편집은 Undo 로 되돌릴 수 있습니다.
    /// 연결 슬롯: -1 = NextId, 0 이상 = ChoiceIds 인덱스 (개수와 같으면 새 선택지 추가).
    /// BuildSaveJson 은 바뀐 행만 table_edit.py 요청 형식으로 만듭니다.
    /// </summary>
    public static class DialogueEdits
    {
        public const int NextSlot = -1;

        public static void Record(DialogueTableSource source, string label, Action edit)
        {
            Undo.RecordObject(source, label);
            edit();
            source.MarkChanged();
        }

        /// <summary>
        /// 블록의 빈 번호 중 가장 작은 DataId 로 행을 만들고 대사 키도 만듭니다. 블록이 차면 -1.
        /// isRouter 면 대사 없는 분기 행을 만듭니다 (후보를 연결하면 조건을 만족하는 첫 후보로 넘어감).
        /// 화자: isPlayer 면 플레이어, 아니면 블록에서 처음 나오는 플레이어 아닌 화자.
        /// </summary>
        public static int AddLine(DialogueTableSource source, int block, bool isRouter = false, bool isPlayer = false)
        {
            int first = Mathf.Max(1, block * ToolDefines.DialogueBlockSize);
            int last = (block + 1) * ToolDefines.DialogueBlockSize - 1;
            int id = Enumerable.Range(first, last - first + 1).FirstOrDefault(i => source.GetLine(i) == null);
            if(id == 0)
                return -1;

            string speakerId = null;
            string speakerName = null;
            if(isPlayer)
            {
                speakerId = ToolDefines.DialoguePlayerSpeakerId;
                speakerName = ToolDefines.DialoguePlayerSpeakerName;
            }
            else if(!isRouter)
            {
                DialogueData speaker = source.GetBlockLines(block).FirstOrDefault(l => !string.IsNullOrEmpty(l.speakerId) && l.speakerId != ToolDefines.DialoguePlayerSpeakerId);
                speakerId = speaker?.speakerId;
                speakerName = speaker?.speakerName;
            }
            Record(source, isRouter ? "분기 추가" : "대사 추가", () =>
            {
                var line = new DialogueData { dataId = id, speakerId = speakerId, speakerName = speakerName, text = isRouter ? null : $"@{ToolDefines.DialogueTextKeyPrefix}{id}" };
                int at = source.Lines.FindIndex(l => l.dataId > id);
                source.Lines.Insert(at < 0 ? source.Lines.Count : at, line);
                if(!isRouter)
                    AddText(source, line.text.Substring(1), ToolDefines.DialogueNewLineText);
            });
            return id;
        }

        /// <summary>행을 지우고, 그 행을 가리키던 NextId 와 ChoiceIds 를 정리합니다. 아무도 안 쓰는 행 전용 대사도 지웁니다.</summary>
        public static void DeleteLines(DialogueTableSource source, ICollection<int> ids)
        {
            Record(source, "대사 삭제", () =>
            {
                List<string> texts = source.Lines.Where(l => ids.Contains(l.dataId)).Select(l => l.text).ToList();
                source.Lines.RemoveAll(l => ids.Contains(l.dataId));
                foreach(DialogueData line in source.Lines)
                {
                    if(ids.Contains(line.nextId))
                        line.nextId = -1;
                    if(line.choiceIds != null)
                        line.choiceIds = line.choiceIds.Where(c => !ids.Contains(c)).ToArray();
                }
                foreach(string text in texts)
                {
                    if(IsOwnTextKey(text) && source.CountTextUses(text) == 0)
                        source.DialogueTexts.RemoveAll(t => t.key == text.Substring(1));
                }
            });
        }

        public static void Connect(DialogueTableSource source, int fromId, int slot, int toId)
        {
            DialogueData line = source.GetLine(fromId);
            Record(source, "연결", () =>
            {
                List<int> choices = line.choiceIds?.ToList() ?? new List<int>();
                if(slot == NextSlot)
                    line.nextId = toId;
                else if(slot < choices.Count)
                    choices[slot] = toId;
                else if(choices.Count < ToolDefines.DialogueMaxChoices)
                    choices.Add(toId);
                line.choiceIds = choices.ToArray();
            });
        }

        /// <summary>연결 하나를 끊습니다. 선택지는 슬롯 대신 대상 DataId 로 찾습니다 (여러 개를 한 번에 끊어도 인덱스가 밀리지 않게).</summary>
        public static void Disconnect(DialogueTableSource source, int fromId, int slot, int toId)
        {
            DialogueData line = source.GetLine(fromId);
            if(line == null)
                return;

            Record(source, "연결 끊기", () =>
            {
                if(slot == NextSlot)
                {
                    if(line.nextId == toId)
                        line.nextId = -1;
                }
                else if(line.choiceIds != null)
                {
                    List<int> choices = line.choiceIds.ToList();
                    choices.Remove(toId);
                    line.choiceIds = choices.ToArray();
                }
            });
        }

        public static void SetText(DialogueTableSource source, string key, string ko)
        {
            Record(source, "대사 수정", () => source.GetDialogueText(key).ko = ko);
        }

        /// <summary>공용 키를 쓰는 행에 행 전용 키(dialogue.{DataId})를 만들어 줍니다. 지금 보이는 대사를 복사합니다.</summary>
        public static void SplitText(DialogueTableSource source, DialogueData line)
        {
            string key = $"{ToolDefines.DialogueTextKeyPrefix}{line.dataId}";
            string ko = source.Resolve(line.text);
            Record(source, "대사 분리", () =>
            {
                if(source.GetDialogueText(key) == null)
                    AddText(source, key, string.IsNullOrEmpty(ko) ? ToolDefines.DialogueNewLineText : ko);
                line.text = $"@{key}";
            });
        }

        /// <summary>바뀐 행이 없으면 null.</summary>
        public static string BuildSaveJson(DialogueTableSource source)
        {
            string dialogue = BuildTableEdit(ToolDefines.DialogueTableName, "dataId",
                source.SavedLines.Select(JsonUtility.FromJson<DialogueData>).ToDictionary(l => l.dataId.ToString(), JsonUtility.ToJson),
                source.Lines.ToDictionary(l => l.dataId.ToString(), l => JsonUtility.ToJson(l)), false);
            string text = BuildTableEdit(ToolDefines.DialogueTextTable, "key",
                source.SavedTexts.Select(JsonUtility.FromJson<TextData>).ToDictionary(t => t.key, JsonUtility.ToJson),
                source.DialogueTexts.ToDictionary(t => t.key, t => JsonUtility.ToJson(t)), true);

            string[] edits = new[] { dialogue, text }.Where(e => e != null).ToArray();
            return edits.Length == 0 ? null : $"[{string.Join(",", edits)}]";
        }

        private static bool IsOwnTextKey(string text)
        {
            return text != null && text.StartsWith($"@{ToolDefines.DialogueTextKeyPrefix}") && int.TryParse(text.Substring(ToolDefines.DialogueTextKeyPrefix.Length + 1), out _);
        }

        private static void AddText(DialogueTableSource source, string key, string ko)
        {
            int id = source.DialogueTexts.Count == 0 ? 1 : source.DialogueTexts.Max(t => t.dataId) + 1;
            source.DialogueTexts.Add(new TextData { dataId = id, key = key, ko = ko });
        }

        /// <summary>키 → 행 JSON 으로 저장 시점과 지금을 비교해 table_edit.py 의 표 하나 요청을 만듭니다. 바뀐 게 없으면 null.</summary>
        private static string BuildTableEdit(string table, string keyField, Dictionary<string, string> saved, Dictionary<string, string> current, bool quoteKeys)
        {
            List<string> upsert = current.Where(p => !saved.TryGetValue(p.Key, out string old) || old != p.Value).Select(p => p.Value).ToList();
            List<string> delete = saved.Keys.Where(k => !current.ContainsKey(k)).Select(k => quoteKeys ? $"\"{k}\"" : k).ToList();
            if(upsert.Count == 0 && delete.Count == 0)
                return null;
            return $"{{\"table\":\"{table}\",\"key\":\"{keyField}\",\"upsert\":[{string.Join(",", upsert)}],\"delete\":[{string.Join(",", delete)}]}}";
        }
    }
}
#endif
