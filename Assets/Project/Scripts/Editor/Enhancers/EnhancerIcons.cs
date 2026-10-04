#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor.Enhancers
{
    /// <summary>
    /// 내장 에디터 아이콘 로더. 없는 이름은 에러 로그 없이 null을 캐시합니다.
    /// </summary>
    public static class EnhancerIcons
    {
        private static readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();
        private static Texture2D _gradient;

        /// <summary>
        /// 흰색, 알파 1에서 0으로 가로 감소하는 텍스처. GUI.color로 색을 입혀 사용.
        /// </summary>
        public static Texture2D GetGradient()
        {
            if(_gradient != null)
                return _gradient;

            int width = ToolDefines.HierarchyGradientResolution;
            _gradient = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            for(int x = 0; x < width; x++)
                _gradient.SetPixel(x, 0, new Color(1f, 1f, 1f, 1f - (float)x / (width - 1)));
            _gradient.Apply();
            return _gradient;
        }

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
