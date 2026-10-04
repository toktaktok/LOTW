#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor.Enhancers
{
    /// <summary>
    /// 오브젝트 이름에 맞는 Hierarchy 이름 규칙을 찾습니다. '*'는 임의의 문자열(빈 문자열 포함).
    /// 복제/새 프리팹 인스턴스처럼 키가 바뀌는 오브젝트에도 스타일이 유지되게 합니다.
    /// </summary>
    public static class HierarchyStyleResolver
    {
        public static bool TryMatchRule(string objectName, IReadOnlyList<FolderRule> rules, out FolderRule match)
        {
            foreach(FolderRule rule in rules)
            {
                if(!string.IsNullOrEmpty(rule.name) && IsMatch(objectName, rule.name))
                {
                    match = rule;
                    return true;
                }
            }
            match = default;
            return false;
        }

        public static bool IsMatch(string name, string pattern)
        {
            string[] parts = pattern.Split('*');
            if(parts.Length == 1)
                return string.Equals(name, pattern, StringComparison.OrdinalIgnoreCase);

            string first = parts[0];
            string last = parts[parts.Length - 1];
            if(!name.StartsWith(first, StringComparison.OrdinalIgnoreCase))
                return false;

            int pos = first.Length;
            for(int i = 1; i < parts.Length - 1; i++)
            {
                if(parts[i].Length == 0)
                    continue;
                int index = name.IndexOf(parts[i], pos, StringComparison.OrdinalIgnoreCase);
                if(index < 0)
                    return false;
                pos = index + parts[i].Length;
            }

            return name.Length - last.Length >= pos && name.EndsWith(last, StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif
