#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor.Enhancers
{
    /// <summary>
    /// 폴더 경로에 적용될 스타일을 결정합니다. 우선순위: 직접 지정 > 이름 규칙 > 상위 폴더 상속.
    /// AssetDatabase에 의존하지 않는 순수 로직(테스트 대상)입니다.
    /// </summary>
    public static class FolderStyleResolver
    {
        public delegate bool StyleLookup(string folderPath, out FolderStyle style);

        public static bool TryResolve(string folderPath, StyleLookup lookup, IReadOnlyList<FolderRule> rules, out FolderStyle result)
        {
            result = default;
            if(string.IsNullOrEmpty(folderPath))
                return false;

            if(lookup(folderPath, out result))
                return true;

            if(rules != null && TryMatchRule(GetName(folderPath), rules, out FolderRule rule))
            {
                result = new FolderStyle { color = rule.color, icon = rule.icon };
                return true;
            }

            string parent = GetParent(folderPath);
            while(parent != null)
            {
                if(lookup(parent, out FolderStyle parentStyle) && parentStyle.inherit)
                {
                    result = parentStyle;
                    return true;
                }
                parent = GetParent(parent);
            }

            result = default;
            return false;
        }

        public static bool TryMatchRule(string folderName, IReadOnlyList<FolderRule> rules, out FolderRule match)
        {
            foreach(FolderRule rule in rules)
            {
                if(string.Equals(rule.name, folderName, StringComparison.OrdinalIgnoreCase))
                {
                    match = rule;
                    return true;
                }
            }
            match = default;
            return false;
        }

        private static string GetName(string path)
        {
            int slash = path.LastIndexOf('/');
            return slash < 0 ? path : path.Substring(slash + 1);
        }

        private static string GetParent(string path)
        {
            int slash = path.LastIndexOf('/');
            return slash <= 0 ? null : path.Substring(0, slash);
        }
    }
}
#endif
