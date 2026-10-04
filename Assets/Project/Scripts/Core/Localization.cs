using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data.Table;

namespace Project.Scripts.Core
{
    /// <summary>
    /// '@키' 형식 문자열을 Text 테이블의 현재 언어 텍스트로 바꿉니다.
    /// '@'로 시작하지 않는 문자열은 그대로 반환하므로 기존 리터럴 텍스트와 섞어 쓸 수 있습니다.
    /// 키가 없으면 '@키'를 그대로 보여 누락을 화면에서 바로 알 수 있게 합니다.
    /// </summary>
    public static class Localization
    {
        public const char KeyPrefix = '@';

        private static Dictionary<string, TextData> _table;

        public static string Resolve(string text)
        {
            if(!IsKey(text))
                return text;

            string language = GameInstance.Instance != null ? GameInstance.Instance.Language : null;
            return Resolve(text, GetTable(), language);
        }

        /// <summary>테이블과 언어를 직접 받는 순수 버전 (테스트용).</summary>
        public static string Resolve(string text, IReadOnlyDictionary<string, TextData> table, string language)
        {
            if(!IsKey(text) || table == null)
                return text;

            if(table.TryGetValue(text.Substring(1), out TextData row))
                return row.Get(language);
            return text;
        }

        public static bool IsKey(string text) => !string.IsNullOrEmpty(text) && text[0] == KeyPrefix;

        /// <summary>DataManager 로드가 끝난 뒤에만 캐싱합니다. 로드 전 호출은 null.</summary>
        private static Dictionary<string, TextData> GetTable()
        {
            if(_table != null)
                return _table;
            if(!DataManager.HasInstance || !DataManager.Instance.IsLoaded)
                return null;

            _table = BuildTable(DataManager.Instance.GetRows<TextData>());
            return _table;
        }

        public static Dictionary<string, TextData> BuildTable(IEnumerable<TextData> rows)
        {
            var table = new Dictionary<string, TextData>();
            foreach(TextData row in rows)
            {
                if(string.IsNullOrEmpty(row.key))
                    continue;
                if(table.ContainsKey(row.key))
                    Debug.LogWarning($"[Localization] Duplicate text key '{row.key}' (dataId {row.dataId}); later row wins.");
                table[row.key] = row;
            }
            return table;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _table = null;
        }
    }
}
