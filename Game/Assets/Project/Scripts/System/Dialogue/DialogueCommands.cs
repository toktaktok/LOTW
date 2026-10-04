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
    ///
    /// 액션 (값 생략 시 1):
    ///   setFlag:key[=값]   addFlag:key[=값]   clearFlag:key
    ///   giveItem:id[=수량] takeItem:id[=수량]
    ///   sfx:클립이름       bgm:클립이름        (AudioLibrary 의 클립 이름)
    ///
    /// 분기 행: text 가 비어 있고 choiceIds 가 있으면 조건을 만족하는 첫 행으로 바로 넘어갑니다.
    /// </summary>
    public static class DialogueCommands
    {
        public const char Separator = ';';
        private const char VerbSeparator = ':';
        private const char ValueSeparator = '=';
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

        private static void RunAction(string token, IDialogueContext context)
        {
            if(!TrySplit(token, VerbSeparator, out string verb, out string body))
            {
                Debug.LogWarning($"[DialogueCommands] Invalid action '{token}'.");
                return;
            }

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
                default:
                    Debug.LogWarning($"[DialogueCommands] Unknown action '{verb}' in '{token}'.");
                    break;
            }
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
