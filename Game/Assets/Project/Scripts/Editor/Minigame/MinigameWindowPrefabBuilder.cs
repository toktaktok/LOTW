// PF_MinigameWindow 프리팹과 기본 렌즈 머티리얼을 만들고 SubSystemCollection에 등록한다 (에디터 전용).
// 다시 실행하면 프리팹을 새로 만들어 덮어쓴다. 등록(UIManager.uiPrefabs, MinigameManager)은 없을 때만 추가한다.
using Project.Scripts.Content.UI;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Editor.Minigame
{
    public static class MinigameWindowPrefabBuilder
    {
        public const string LibraryPath = "Assets/Project/Data/Minigames/MinigameLibrary_Main.asset";

        private const string PrefabPath = "Assets/Project/Prefabs/UI/PF_MinigameWindow.prefab";
        private const string MaterialPath = "Assets/Project/Art/Materials/MAT_MinigameLens.mat";
        private const string ShaderName = "LOTW/UI/MinigameLens";
        private const string SubSystemPath = "Assets/Project/Resources/SubSystemCollection.prefab";
        private const string FontPath = "Assets/Project/Art/Fonts/Galmuri11/Galmuri11 Pixel.asset";
        private const int UILayerIndex = 5;

        // 1920x1080 기준 배치
        private const float TitleBarHeight = 44f;
        private const float FramePadding = 14f;
        private const float BorderThickness = 2f;
        private const float TitleMarginLeft = 22f;
        private const float StatusMarginRight = 56f;
        private const float FontSize = 22f;
        private static readonly Vector2 CloseSize = new Vector2(32f, 32f);
        private static readonly Vector2 ClosePosition = new Vector2(-8f, -6f);

        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.25f);
        private static readonly Color FrameColor = new Color(0.09f, 0.11f, 0.13f, 0.55f);
        private static readonly Color BorderColor = new Color(0.92f, 0.94f, 0.95f, 0.6f);
        private static readonly Color TitleBarColor = new Color(0.92f, 0.94f, 0.95f, 0.12f);
        private static readonly Color TextColor = new Color(0.95f, 0.96f, 0.97f, 1f);
        private static readonly Color CloseColor = new Color(0.92f, 0.94f, 0.95f, 0.85f);
        private static readonly Color CloseLabelColor = new Color(0.09f, 0.11f, 0.13f, 1f);

        [MenuItem("LOTW/Minigame/1 Build Window Prefab")]
        public static void Run()
        {
            Material material = GetOrCreateMaterial();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if(font == null)
                Debug.LogWarning($"[MinigameWindowPrefabBuilder] Font not found: {FontPath}");

            GameObject root = BuildWindow(material, font);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            RegisterInSubSystem();
            AssetDatabase.SaveAssets();
        }

        public static MinigameLibrary GetOrCreateLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<MinigameLibrary>(LibraryPath);
            if(library != null)
                return library;

            EnsureFolder(LibraryPath);
            library = ScriptableObject.CreateInstance<MinigameLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
            return library;
        }

        /// <summary>에셋 경로의 상위 폴더가 없으면 만든다.</summary>
        public static void EnsureFolder(string assetPath)
        {
            string folder = assetPath.Substring(0, assetPath.LastIndexOf('/'));
            if(AssetDatabase.IsValidFolder(folder))
                return;

            string parent = folder.Substring(0, folder.LastIndexOf('/'));
            EnsureFolder(folder);
            AssetDatabase.CreateFolder(parent, folder.Substring(parent.Length + 1));
        }

        private static Material GetOrCreateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if(material != null)
                return material;

            material = new Material(Shader.Find(ShaderName));
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static GameObject BuildWindow(Material material, TMP_FontAsset font)
        {
            RectTransform root = CreateRect(null, "PF_MinigameWindow");
            Stretch(root, Vector2.zero, Vector2.zero);
            root.gameObject.AddComponent<CanvasGroup>();

            // 뒤쪽 월드를 살짝 어둡게 하고 창 밖 클릭을 막음
            Image backdrop = CreateImage(root, "Backdrop", BackdropColor);
            Stretch(backdrop.rectTransform, Vector2.zero, Vector2.zero);

            RectTransform window = CreateRect(root, "Window");

            Image frame = CreateImage(window, "Frame", FrameColor);
            Stretch(frame.rectTransform, Vector2.zero, Vector2.zero);
            CreateBorder(frame.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, BorderThickness));
            CreateBorder(frame.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, BorderThickness));
            CreateBorder(frame.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(BorderThickness, 0f));
            CreateBorder(frame.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(BorderThickness, 0f));

            Image titleBar = CreateImage(frame.transform, "TitleBar", TitleBarColor);
            titleBar.raycastTarget = false;
            StretchTop(titleBar.rectTransform, 0f, 0f);

            RawImage screen = CreateRect(window, "Screen").gameObject.AddComponent<RawImage>();
            screen.raycastTarget = false;
            Stretch(screen.rectTransform, new Vector2(FramePadding, FramePadding), new Vector2(-FramePadding, -(FramePadding + TitleBarHeight)));

            TextMeshProUGUI title = CreateText(window, "Title", font, TextAlignmentOptions.Left);
            StretchTop(title.rectTransform, TitleMarginLeft, 0f);
            TextMeshProUGUI status = CreateText(window, "Status", font, TextAlignmentOptions.Right);
            StretchTop(status.rectTransform, 0f, -StatusMarginRight);

            Image close = CreateImage(window, "Close", CloseColor);
            close.rectTransform.anchorMin = close.rectTransform.anchorMax = close.rectTransform.pivot = Vector2.one;
            close.rectTransform.sizeDelta = CloseSize;
            close.rectTransform.anchoredPosition = ClosePosition;
            Button closeButton = close.gameObject.AddComponent<Button>();
            TextMeshProUGUI closeLabel = CreateText(close.transform, "Label", font, TextAlignmentOptions.Center);
            closeLabel.text = "X";
            closeLabel.color = CloseLabelColor;
            Stretch(closeLabel.rectTransform, Vector2.zero, Vector2.zero);

            var ui = root.gameObject.AddComponent<MinigameWindow>();
            var serialized = new SerializedObject(ui);
            serialized.FindProperty("window").objectReferenceValue = window;
            serialized.FindProperty("screen").objectReferenceValue = screen;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("statusText").objectReferenceValue = status;
            serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
            serialized.FindProperty("defaultScreenMaterial").objectReferenceValue = material;
            // 열림/닫힘은 MinigameWindow가 DOTween으로 직접 처리
            serialized.FindProperty("transitionMode").enumValueIndex = (int)UITransitionMode.None;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return root.gameObject;
        }

        private static void RegisterInSubSystem()
        {
            var windowPrefab = AssetDatabase.LoadAssetAtPath<MinigameWindow>(PrefabPath);
            MinigameLibrary library = GetOrCreateLibrary();

            GameObject root = PrefabUtility.LoadPrefabContents(SubSystemPath);
            try
            {
                var uiManager = root.GetComponentInChildren<UIManager>(true);
                var serializedUI = new SerializedObject(uiManager);
                SerializedProperty prefabs = serializedUI.FindProperty("uiPrefabs");
                bool registered = false;
                for(int i = 0; i < prefabs.arraySize; i++)
                    registered |= prefabs.GetArrayElementAtIndex(i).objectReferenceValue == windowPrefab;
                if(!registered)
                {
                    prefabs.arraySize++;
                    prefabs.GetArrayElementAtIndex(prefabs.arraySize - 1).objectReferenceValue = windowPrefab;
                    serializedUI.ApplyModifiedPropertiesWithoutUndo();
                }

                var manager = root.GetComponentInChildren<MinigameManager>(true);
                if(manager == null)
                    manager = uiManager.gameObject.AddComponent<MinigameManager>();
                var serializedManager = new SerializedObject(manager);
                serializedManager.FindProperty("library").objectReferenceValue = library;
                serializedManager.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, SubSystemPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        #region UI helpers

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UILayerIndex };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            Image image = CreateRect(parent, name).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void CreateBorder(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 thickness)
        {
            Image border = CreateImage(parent, "Border", BorderColor);
            border.raycastTarget = false;
            border.rectTransform.anchorMin = anchorMin;
            border.rectTransform.anchorMax = anchorMax;
            border.rectTransform.sizeDelta = thickness;
            border.rectTransform.anchoredPosition = Vector2.zero;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, TMP_FontAsset font, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = CreateRect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
            if(font != null)
                text.font = font;
            text.fontSize = FontSize;
            text.alignment = alignment;
            text.color = TextColor;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        // 부모 위쪽에 제목 줄 높이로 붙임
        private static void StretchTop(RectTransform rect, float left, float right)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, -TitleBarHeight);
            rect.offsetMax = new Vector2(right, 0f);
        }

        #endregion
    }
}
