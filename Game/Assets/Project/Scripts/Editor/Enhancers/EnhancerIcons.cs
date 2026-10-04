#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Project.Scripts.Editor.Enhancers
{
    /// <summary>
    /// 내장 에디터 아이콘 로더. 없는 이름은 에러 로그 없이 null을 캐시합니다.
    /// </summary>
    public static class EnhancerIcons
    {
        private static readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();

        public static Texture2D Get(string name)
        {
            if(string.IsNullOrEmpty(name))
                return null;
            if(_cache.TryGetValue(name, out Texture2D cached))
                return cached;

            Texture2D texture = null;
            if(EditorGUIUtility.isProSkin)
                texture = EditorGUIUtility.FindTexture("d_" + name);
            if(texture == null)
                texture = EditorGUIUtility.FindTexture(name);

            _cache[name] = texture;
            return texture;
        }
    }
}
#endif
