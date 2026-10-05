using System;
using UnityEngine;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;

namespace Project.Scripts.System.Dialogue
{
    /// <summary>
    /// Dialogue 테이블의 conditions / actions 컬럼을 해석합니다. 여러 항목은 ';' 로 구분합니다.
    ///
    /// 조건 (모두 만족해야 통과, 앞에 '!' 를 붙이면 반대):
    ///   flag:key          플래그가 0이 아님
    ///   flag:key>=2       비교 연산 >=, <=, ==, !=, >, <, = (=는 ==)
    ///   item:rose         아이템을 1개 이상 보유
    ///   item:rose>=3      보유 수량 비교
    ///   quest:101         의뢰 상태가 None 아님. quest:101=2 처럼 비교 (1 진행, 2 완료, 3 잠금)
    ///   note:201          수첩 항목 해금됨. note:201=3 은 취소선 (1 안읽음, 2 읽음, 3 취소선)
    ///   met:npc_gumman    첫 만남 이후
    ///   var:jumps>=5      미니게임 변수 비교 (미니게임 정의의 조건에서만 사용)
    ///
    /// 액션 (값 생략 시 1):
    ///   setFlag:key[=값]   addFlag:key[=값]   clearFlag:key
    ///   giveItem:id[=수량] takeItem:id[=수량]
    ///   sfx:클립이름       bgm:클립이름        (AudioLibrary 의 클립 이름)
    ///   startQuest:id      completeQuest:id    (진행 / 완료. 시작은 상태가 None 일 때만)
    ///   addNote:id         strikeNote:id       (수첩 해금 / 무관 판정 취소선. 해금은 처음 한 번만)
    ///   meet:characterId   (첫 만남 기록 + metCount 증가. 두 번째부터는 무시)
    ///   sequence:id        (대화가 닫힌 뒤 Sequence 테이블 연출 재생)
    ///   save               (현재 슬롯에 저장. 값 없이 쓰는 유일한 동사)
    /// 의뢰/수첩/만남 상태는 플래그(StoryKeys)라서 세이브에 같이 저장됩니다.
    ///   minigame:id        대화를 닫고 미니게임을 연 뒤, 끝나면 다음 행(또는 결과의 followDialogueId)에서 대화를 이어감
    ///
    /// 분기 행: text 가 비어 있고 choiceIds 가 있으면 조건을 만족하는 첫 행으로 바로 넘어갑니다.
    /// </summary>
    public static class DialogueCommands
    {
        public const char Separator = ';';
        private const char VerbSeparator = ':';
        private const char ValueSeparator = '=';
        private const string SaveVerb = "save";
        private const string MinigameVerb = "minigame";
        private static readonly char[] OperatorChars = { '>', '<', '=', '!' };

        #region Conditions

        /// <summary>비어 있으면 true. 해석할 수 없는 조건은 경고 후 false.</summary>
        public static bool CheckConditions(string conditions, IDialogueContext context)
        {
            if(string.IsNullOrWhiteSpace(conditions))
                return true;

            foreach(string raw in conditions.Split(Separator))
            {
                string token = raw.Trim();
                if(token.Length == 0)
                    continue;
                if(!CheckCondition(token, context))
                    return false;
            }
            return true;
        }

        private static bool CheckCondition(string token, IDialogueContext context)
        {
            bool negate = token[0] == '!';
            if(negate)
                token = token.Substring(1).Trim();

            if(!TrySplit(token, VerbSeparator, out string type, out string body) ||
               !TryParseComparison(body, out string key, out string op, out int value))
            {
                Debug.LogWarning($"[DialogueCommands] Invalid condition '{token}'.");
                return false;
            }

            int current;
            switch(type.ToLowerInvariant())
            {
                case "flag":
                    current = context.GetFlag(key);
                    break;
                case "item":
                    current = context.GetItemCount(key);
                    break;
                case "quest":
                    current = context.GetFlag(StoryKeys.QuestPrefix + key);
                    break;
                case "note":
                    current = context.GetFlag(StoryKeys.NotePrefix + key);
                    break;
                case "met":
                    current = context.GetFlag(StoryKeys.MetPrefix + key);
                    break;
                case "var":
                    if(context is not IVariableContext variables)
                    {
                        Debug.LogWarning($"[DialogueCommands] Condition '{token}' needs a variable context (minigame only).");
                        return false;
                    }
                    current = variables.GetVar(key);
                    break;
                default:
                    Debug.LogWarning($"[DialogueCommands] Unknown condition type '{type}' in '{token}'.");
                    return false;
            }

            bool result = Compare(current, op, value);
            return negate ? !result : result;
        }

        /// <summary>"key" -> (key, "!=", 0), "key>=2" -> (key, ">=", 2).</summary>
        private static bool TryParseComparison(string body, out string key, out string op, out int value)
        {
            int opIndex = body.IndexOfAny(OperatorChars);
            if(opIndex < 0)
            {
                key = body.Trim();
                op = "!=";
                value = 0;
                return key.Length > 0;
            }

            key = body.Substring(0, opIndex).Trim();
            int valueIndex = opIndex;
            while(valueIndex < body.Length && Array.IndexOf(OperatorChars, body[valueIndex]) >= 0)
                valueIndex++;
            op = body.Substring(opIndex, valueIndex - opIndex);
            if(op == "=")
                op = "==";

            bool valid = op is ">=" or "<=" or "==" or "!=" or ">" or "<";
            return int.TryParse(body.Substring(valueIndex).Trim(), out value) && valid && key.Length > 0;
        }

        private static bool Compare(int current, string op, int value)
        {
            return op switch
            {
                ">=" => current >= value,
                "<=" => current <= value,
                ">" => current > value,
                "<" => current < value,
                "==" => current == value,
                _ => current != value,
            };
        }

        #endregion

        #region Actions

        public static void RunActions(string actions, IDialogueContext context)
        {
            if(string.IsNullOrWhiteSpace(actions))
                return;

            foreach(string raw in actions.Split(Separator))
            {
                string token = raw.Trim();
                if(token.Length > 0)
                    RunAction(token, context);
            }
        }

        /// <summary>액션 목록에 'minigame:id' 가 있으면 그 id를 돌려줍니다. 실제 시작은 DialogueUI가 처리합니다.</summary>
        public static bool TryGetMinigameId(string actions, out string id)
        {
            id = null;
            if(string.IsNullOrWhiteSpace(actions))
                return false;

            foreach(string raw in actions.Split(Separator))
            {
                if(TrySplit(raw.Trim(), VerbSeparator, out string verb, out string body) && verb.ToLowerInvariant() == MinigameVerb)
                {
                    id = body;
                    return true;
                }
            }
            return false;
        }

        private static void RunAction(string token, IDialogueContext context)
        {
            if(!TrySplit(token, VerbSeparator, out string verb, out string body) && !string.Equals(verb, SaveVerb, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[DialogueCommands] Invalid action '{token}'.");
                return;
            }

            // 게임 상태를 바꾸지 않는 흐름 액션. TryGetMinigameId로 읽어 DialogueUI가 실행함
            if(verb.ToLowerInvariant() == MinigameVerb)
                return;

            body ??= string.Empty;
            string key = body;
            int amount = 1;
            if(TrySplit(body, ValueSeparator, out string left, out string right))
            {
                key = left;
                if(!int.TryParse(right, out amount))
                {
                    Debug.LogWarning($"[DialogueCommands] Invalid value in action '{token}'.");
                    return;
                }
            }

            switch(verb.ToLowerInvariant())
            {
                case "setflag":
                    context.SetFlag(key, amount);
                    break;
                case "addflag":
                    context.AddFlag(key, amount);
                    break;
                case "clearflag":
                    context.SetFlag(key, 0);
                    break;
                case "giveitem":
                    context.AddItem(key, amount);
                    break;
                case "takeitem":
                    context.RemoveItem(key, amount);
                    break;
                case "sfx":
                    context.PlaySfx(key);
                    break;
                case "bgm":
                    context.PlayBgm(key);
                    break;
                case "startquest":
                    SetIfUnset(context, StoryKeys.QuestPrefix + key, (int)QuestState.Active);
                    break;
                case "completequest":
                    context.SetFlag(StoryKeys.QuestPrefix + key, (int)QuestState.Done);
                    break;
                case "addnote":
                    SetIfUnset(context, StoryKeys.NotePrefix + key, (int)NoteState.Unread);
                    break;
                case "strikenote":
                    context.SetFlag(StoryKeys.NotePrefix + key, (int)NoteState.Struck);
                    break;
                case "meet":
                    if(SetIfUnset(context, StoryKeys.MetPrefix + key, 1))
                        context.AddFlag(StoryKeys.MetCount, 1);
                    break;
                case "sequence":
                    if(int.TryParse(key, out int sequenceId))
                        context.PlaySequence(sequenceId);
                    else
                        Debug.LogWarning($"[DialogueCommands] Invalid sequence id in '{token}'.");
                    break;
                case SaveVerb:
                    context.SaveGame();
                    break;
                default:
                    Debug.LogWarning($"[DialogueCommands] Unknown action '{verb}' in '{token}'.");
                    break;
            }
        }

        /// <summary>값이 0 일 때만 설정. 이미 진행된 상태를 되돌리지 않기 위함. 설정했으면 true.</summary>
        private static bool SetIfUnset(IDialogueContext context, string key, int value)
        {
            if(context.GetFlag(key) != 0)
                return false;

            context.SetFlag(key, value);
            return true;
        }

        #endregion

        #region Routing

        public static bool IsRouter(DialogueData line)
        {
            return line != null && string.IsNullOrEmpty(line.text) && line.choiceIds != null && line.choiceIds.Length > 0;
        }

        /// <summary>
        /// 분기 행이면 액션을 실행하고 조건을 만족하는 첫 choiceIds 행(없으면 nextId)으로 이동하기를 반복해,
        /// 실제로 표시할 행을 반환합니다. 대화가 끝나면 null.
        /// </summary>
        public static DialogueData ResolveRoute(DialogueData line, Func<int, DialogueData> getLine, IDialogueContext context)
        {
            for(int depth = 0; IsRouter(line); depth++)
            {
                if(depth >= DialogueDefines.MaxRouteDepth)
                {
                    Debug.LogWarning($"[DialogueCommands] Route depth exceeded at dataId {line.dataId}; check for a router loop.");
                    return null;
                }

                RunActions(line.actions, context);
                line = PickRoute(line, getLine, context);
            }
            return line;
        }

        private static DialogueData PickRoute(DialogueData router, Func<int, DialogueData> getLine, IDialogueContext context)
        {
            foreach(int id in router.choiceIds)
            {
                DialogueData candidate = getLine(id);
                if(candidate != null && CheckConditions(candidate.conditions, context))
                    return candidate;
            }
            return getLine(router.nextId);
        }

        #endregion

        private static bool TrySplit(string text, char separator, out string left, out string right)
        {
            int index = text.IndexOf(separator);
            if(index <= 0)
            {
                left = text.Trim();
                right = null;
                return false;
            }

            left = text.Substring(0, index).Trim();
            right = text.Substring(index + 1).Trim();
            return left.Length > 0 && right.Length > 0;
        }
    }
}
