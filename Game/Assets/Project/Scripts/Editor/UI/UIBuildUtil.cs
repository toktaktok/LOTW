// UI 프리팹 빌더 공용 함수 (에디터 전용). NotebookUIBuilder, MenuUIBuilder 가 `using static` 으로 씁니다.
using System;
using Project.Scripts.System.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Project.Scripts.Framework.Managers;
using Project.Scripts.Framework.UI;

namespace Project.Scripts.Editor.UI
{
    public static class UIBuildUtil
    {
        public const string FontPath = "Assets/Project/Art/Fonts/Galmuri11/Galmuri11 Pixel.asset";
        public const string SubSystemPrefabPath = "Assets/Project/Resources/SubSystemCollection.prefab";
        public const int UILayerIndex = 5;
        public const float DefaultButtonFontSize = 22f;

        // 중간 톤 크래프트 브라운 계열 (크림/보라 금지)
        public static readonly Color TextColor = new Color(0.23f, 0.17f, 0.16f, 1f);
        public static readonly Color KraftColor = new Color(0.66f, 0.51f, 0.36f, 1f);
        public static readonly Color KraftPanelColor = new Color(0.66f, 0.51f, 0.36f, 0.88f);
        public static readonly Color PaperColor = new Color(0.80f, 0.68f, 0.53f, 1f);
        public static readonly Color DimColor = new Color(0f, 0f, 0f, 0.55f);

        public static TMP_FontAsset LoadFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if(font == null)
                Debug.LogWarning($"[UIBuildUtil] Font not found: {FontPath}");
            return font;
        }

        public static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(sprite == null)
                Debug.LogWarning($"[UIBuildUtil] Sprite not found: {path}");
            return sprite;
        }

        /// <summary>전체 화면 루트(CanvasGroup 포함). 저장 후 DestroyImmediate 는 호출한 쪽이 합니다.</summary>
        public static GameObject CreateRoot(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            go.layer = UILayerIndex;
            Stretch((RectTransform)go.transform);
            return go;
        }

        public static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UILayerIndex;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform CreatePanel(string name, Transform parent, Vector2 size, Color color)
        {
            RectTransform rect = CreateUIObject(name, parent);
            rect.sizeDelta = size;
            rect.gameObject.AddComponent<Image>().color = color;
            return rect;
        }

        public static TMP_Text CreateText(string name, RectTransform parent, TMP_FontAsset font, float size, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateUIObject(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if(font != null)
                text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = TextColor;
            text.text = string.Empty;
            return text;
        }

        /// <summary>'@키' 라벨. LocalizedText 가 실행 중 현재 언어로 바꿉니다.</summary>
        public static TMP_Text CreateLocalizedText(string name, RectTransform parent, TMP_FontAsset font, float size, TextAlignmentOptions alignment, string key)
        {
            TMP_Text text = CreateText(name, parent, font, size, alignment);
            text.raycastTarget = false;
            SetLocalizedKey(text, key);
            return text;
        }

        public static void SetLocalizedKey(TMP_Text text, string key)
        {
            var localized = text.GetComponent<LocalizedText>();
            if(localized == null)
                localized = text.gameObject.AddComponent<LocalizedText>();
            var so = new SerializedObject(localized);
            so.FindProperty("key").stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>단색 사각 버튼 + 가운데 라벨. label 이 '@' 로 시작하면 LocalizedText 를 붙입니다.</summary>
        public static Button CreateTextButton(string name, RectTransform parent, TMP_FontAsset font, string label, Vector2 size, float fontSize = DefaultButtonFontSize)
        {
            RectTransform rect = CreateUIObject(name, parent);
            rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = KraftColor;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            TMP_Text text = CreateText("Label", rect, font, fontSize, TextAlignmentOptions.Center);
            Stretch((RectTransform)text.transform);
            text.raycastTarget = false;
            if(label.StartsWith("@"))
                SetLocalizedKey(text, label);
            else
                text.text = label;
            return button;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void PlaceTop(RectTransform rect, float top, float height)
        {
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(0f, height);
        }

        public static void Anchor(RectTransform rect, Vector2 corner)
        {
            rect.anchorMin = corner;
            rect.anchorMax = corner;
            rect.pivot = corner;
        }

        public static VerticalLayoutGroup AddVerticalLayout(RectTransform rect, float spacing, bool controlHeight)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = controlHeight;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static void Set(SerializedObject so, string field, UnityEngine.Object value)
        {
            SerializedProperty prop = so.FindProperty(field);
            if(prop == null)
            {
                Debug.LogError($"[UIBuildUtil] Field not found: {so.targetObject.GetType().Name}.{field}");
                return;
            }
            prop.objectReferenceValue = value;
        }

        public static void SetArray<T>(SerializedObject so, string field, T[] values) where T : UnityEngine.Object
        {
            SerializedProperty prop = so.FindProperty(field);
            if(prop == null)
            {
                Debug.LogError($"[UIBuildUtil] Field not found: {so.targetObject.GetType().Name}.{field}");
                return;
            }
            prop.arraySize = values.Length;
            for(int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        /// <summary>
        /// prefabPath 에 프리팹이 없을 때만 build 로 만든 루트를 저장합니다. 이미 있으면 그대로 반환(수작업 보존).
        /// </summary>
        public static T BuildPrefabOnce<T>(string prefabPath, Func<GameObject> build) where T : Component
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(prefabPath);
            if(existing != null)
            {
                Debug.Log($"[UIBuildUtil] {prefabPath} already exists, skipped. Delete it to rebuild.");
                return existing;
            }

            GameObject root = build();
            try
            {
                return PrefabUtility.SaveAsPrefabAsset(root, prefabPath).GetComponent<T>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>SubSystemCollection 의 UIManager.uiPrefabs 에 없으면 추가합니다.</summary>
        public static void RegisterUIPrefabs(params BaseUI[] prefabs)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SubSystemPrefabPath);
            try
            {
                var manager = root.GetComponentInChildren<UIManager>(true);
                if(manager == null)
                {
                    Debug.LogError("[UIBuildUtil] UIManager not found in SubSystemCollection");
                    return;
                }

                var so = new SerializedObject(manager);
                SerializedProperty list = so.FindProperty("uiPrefabs");
                bool changed = false;
                foreach(BaseUI prefab in prefabs)
                {
                    if(prefab == null || Contains(list, prefab))
                        continue;

                    list.arraySize++;
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = prefab;
                    changed = true;
                }

                if(!changed)
                    return;

                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, SubSystemPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool Contains(SerializedProperty list, UnityEngine.Object value)
        {
            for(int i = 0; i < list.arraySize; i++)
            {
                if(list.GetArrayElementAtIndex(i).objectReferenceValue == value)
                    return true;
            }
            return false;
        }
    }
}
