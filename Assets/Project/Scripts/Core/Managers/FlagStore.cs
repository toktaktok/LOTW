using System.Collections.Generic;

namespace Project.Scripts.Core.Managers
{
    /// <summary>
    /// 게임 진행 플래그(스토리/퀘스트/월드 상태)를 보관하는 순수 C# 저장소입니다.
    /// 키 -> 정수 값으로 관리하며, 단순 ON/OFF 플래그는 값 1, 미설정은 0으로 취급합니다.
    /// MonoBehaviour 의존이 없어 EditMode 테스트에서 단독 검증이 가능합니다.
    /// 직렬화는 SaveData.flags(string[])와 호환되도록 "key=value" 형식을 사용합니다.
    /// </summary>
    public class FlagStore
    {
        private readonly Dictionary<string, int> _flags = new();

        /// <summary>현재 보관 중인 플래그 수.</summary>
        public int Count => _flags.Count;

        /// <summary>
        /// 플래그 값을 설정합니다. value가 0이면 플래그를 제거합니다(미설정과 동일).
        /// 빈 키는 무시합니다.
        /// </summary>
        public void Set(string key, int value = 1)
        {
            if (string.IsNullOrEmpty(key)) return;

            if (value == 0)
                _flags.Remove(key);
            else
                _flags[key] = value;
        }

        /// <summary>플래그가 0이 아닌 값으로 설정되어 있는지 여부.</summary>
        public bool Has(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return _flags.TryGetValue(key, out int value) && value != 0;
        }

        /// <summary>플래그 값을 반환합니다. 미설정이면 0.</summary>
        public int Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0;
            return _flags.TryGetValue(key, out int value) ? value : 0;
        }

        /// <summary>플래그 값에 amount를 더한 뒤 결과 값을 반환합니다. 진행 카운터용.</summary>
        public int Add(string key, int amount = 1)
        {
            if (string.IsNullOrEmpty(key)) return 0;

            int next = Get(key) + amount;
            Set(key, next);
            return next;
        }

        /// <summary>특정 플래그를 제거합니다.</summary>
        public void Clear(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _flags.Remove(key);
        }

        /// <summary>모든 플래그를 제거합니다.</summary>
        public void ClearAll()
        {
            _flags.Clear();
        }

        #region Save/Load Integration

        /// <summary>
        /// SaveData.flags 형식("key=value")으로 직렬화합니다.
        /// </summary>
        public string[] ToSaveData()
        {
            var result = new string[_flags.Count];
            int i = 0;
            foreach (var pair in _flags)
            {
                result[i] = $"{pair.Key}={pair.Value}";
                i++;
            }
            return result;
        }

        /// <summary>
        /// SaveData.flags 배열로부터 상태를 복원합니다.
        /// 잘못된 형식의 항목과 빈 키는 무시합니다.
        /// 값이 없는 항목("key")은 1로 간주합니다.
        /// </summary>
        public void LoadFromSaveData(string[] data)
        {
            _flags.Clear();
            if (data == null) return;

            foreach (var entry in data)
            {
                if (string.IsNullOrEmpty(entry)) continue;

                int sep = entry.IndexOf('=');
                string key;
                int value;

                if (sep < 0)
                {
                    key = entry;
                    value = 1;
                }
                else
                {
                    key = entry.Substring(0, sep);
                    if (!int.TryParse(entry.Substring(sep + 1), out value))
                        continue;
                }

                if (string.IsNullOrEmpty(key) || value == 0) continue;
                _flags[key] = value;
            }
        }

        #endregion
    }
}
