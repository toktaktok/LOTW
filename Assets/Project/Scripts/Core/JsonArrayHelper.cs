using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Core
{
    /// <summary>
    /// Unity JsonUtility 는 최상위 JSON 배열을 직접 파싱할 수 없어서
    /// {"items":[...]} 로 감싼 뒤 역직렬화하는 헬퍼입니다.
    ///
    /// 사용 예:
    ///   List&lt;DialogueData&gt; rows = JsonArrayHelper.FromJson&lt;DialogueData&gt;(jsonText);
    /// </summary>
    public static class JsonArrayHelper
    {
        // Unity 직렬화 규칙상 [Serializable] 이 붙은 구체 타입이어야 합니다.
        // 제네릭 클래스는 호출 지점에서 타입이 확정될 때 정상 동작합니다.
        [Serializable]
        private class ArrayWrapper<T>
        {
            public List<T> items;
        }

        /// <summary>JSON 배열 문자열 → List&lt;T&gt; 로 역직렬화합니다.</summary>
        public static List<T> FromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new List<T>();

            string wrapped = $"{{\"items\":{json}}}";
            ArrayWrapper<T> wrapper = JsonUtility.FromJson<ArrayWrapper<T>>(wrapped);
            return wrapper?.items ?? new List<T>();
        }
    }
}
