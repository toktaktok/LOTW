#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Project.Scripts.Data;
using Project.Scripts.Editor.Data;
using Project.Scripts.System.Dialogue;

namespace Project.Scripts.Editor.Dialogue
{
    /// <summary>
    /// 조건/액션 문자열을 한 줄에 한 항목씩 편집합니다 (IMGUI). 줄: [아님] 종류, 키(직접 입력 또는 목록), 연산자, 값.
    /// 문법은 DialogueCommands 를 따르고, 문자열 변환은 DialogueCommands.Parse / Format 이 합니다.
    /// </summary>
    public static class CommandListField
    {
        private static readonly string[] ConditionTypes = { "flag", "item" };
        private static readonly string[] ActionVerbs = { "setFlag", "addFlag", "clearFlag", "giveItem", "takeItem", "sfx", "bgm", "minigame" };
        private static readonly string[] Operators = { "", ">=", "<=", "==", "!=", ">", "<" };
        // 값을 받는 액션 (비우면 1)
        private static readonly string[] ValueVerbs = { "setFlag", "addFlag", "giveItem", "takeItem" };

        /// <summary>바뀌었으면 새 문자열, 아니면 text 를 그대로 돌려줍니다.</summary>
        public static string Draw(string label, string text, bool isCondition, CommandPickers pickers)
        {
            List<DialogueCommand> commands = DialogueCommands.Parse(text);
            string[] verbs = isCondition ? ConditionTypes : ActionVerbs;

            EditorGUILayout.LabelField(label);
            EditorGUI.BeginChangeCheck();
            for(int i = 0; i < commands.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                commands[i] = DrawCommand(commands[i], isCondition, verbs, pickers);
                bool remove = GUILayout.Button("x", GUILayout.Width(ToolDefines.DialogueCommandRemoveWidth));
                EditorGUILayout.EndHorizontal();
                if(remove)
                {
                    commands.RemoveAt(i);
                    break;
                }
            }
            if(GUILayout.Button(isCondition ? "+ 조건" : "+ 실행"))
                commands.Add(new DialogueCommand { verb = verbs[0], key = string.Empty, op = string.Empty, value = string.Empty });
            return EditorGUI.EndChangeCheck() ? DialogueCommands.Format(commands) : text;
        }

        private static DialogueCommand DrawCommand(DialogueCommand command, bool isCondition, string[] verbs, CommandPickers pickers)
        {
            if(isCondition)
                command.negate = GUILayout.Toggle(command.negate, "아님", GUILayout.Width(ToolDefines.DialogueCommandNegateWidth));

            // 목록에 없는 종류(var, 오타)도 그대로 보이게 끝에 붙임
            int verbIndex = Array.FindIndex(verbs, v => string.Equals(v, command.verb, StringComparison.OrdinalIgnoreCase));
            string[] verbOptions = verbIndex < 0 ? verbs.Append(command.verb).ToArray() : verbs;
            int pickedVerb = EditorGUILayout.Popup(verbIndex < 0 ? verbs.Length : verbIndex, verbOptions, GUILayout.Width(ToolDefines.DialogueCommandVerbWidth));
            command.verb = verbOptions[pickedVerb];

            command.key = EditorGUILayout.TextField(command.key);
            string[] keys = pickers.GetKeys(command.verb);
            int pickedKey = EditorGUILayout.Popup(Array.IndexOf(keys, command.key), keys, GUILayout.Width(ToolDefines.DialogueCommandPickWidth));
            if(pickedKey >= 0)
                command.key = keys[pickedKey];

            bool hasValue;
            if(isCondition)
            {
                int opIndex = Array.IndexOf(Operators, command.op);
                string[] opOptions = opIndex < 0 ? Operators.Append(command.op).ToArray() : Operators;
                command.op = opOptions[EditorGUILayout.Popup(opIndex < 0 ? Operators.Length : opIndex, opOptions, GUILayout.Width(ToolDefines.DialogueCommandOpWidth))];
                hasValue = command.op.Length > 0;
            }
            else
            {
                hasValue = ValueVerbs.Any(v => string.Equals(v, command.verb, StringComparison.OrdinalIgnoreCase));
            }

            if(hasValue)
                command.value = EditorGUILayout.TextField(command.value, GUILayout.Width(ToolDefines.DialogueCommandValueWidth));
            else
                command.value = string.Empty;
            // 액션 값은 "=값", 비우면 연산자도 뺌 (값 생략 = 1)
            if(!isCondition)
                command.op = command.value.Length > 0 ? "=" : string.Empty;
            return command;
        }
    }
}
#endif
