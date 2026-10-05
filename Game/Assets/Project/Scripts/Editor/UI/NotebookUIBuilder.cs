// 탐정 수첩 UI 프리팹 생성 (에디터 전용). PF_NotebookUI 를 만들고, PF_HudUI 에 수첩 버튼/의뢰 목록/알림을 붙이고,
// SubSystemCollection 의 UIManager.uiPrefabs 에 NotebookUI 를 등록한다. 이미 있는 것은 새로 만들지 않고 연결만 다시 한다.
using System;
using Project.Scripts.Content.UI;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.System.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Editor.UI
{
    public static class NotebookUIBuilder
    {
        private const string NotebookPrefabPath = "Assets/Project/Prefabs/UI/PF_NotebookUI.prefab";
        private const string HudPrefabPath = "Assets/Project/Prefabs/UI/PF_HudUI.prefab";
        private const string SubSystemPrefabPath = "Assets/Project/Resources/SubSystemCollection.prefab";
        private const string InnerSpritePath = "Assets/Project/Art/UI/TX_UI_Notebook_Inner.png";
        private const string CoverSpritePath = "Assets/Project/Art/UI/TX_UI_Notebook_Cover.png";
        private const string ButtonSpritePath = "Assets/Project/Art/UI/TX_UI_NotebookButton.png";
        private const string YellowStickyPath = "Assets/Project/Art/UI/TX_UI_StickyNote_Yellow.png";
        private const string GreenStickyPath = "Assets/Project/Art/UI/TX_UI_StickyNote_Green.png";
        private const string FontPath = "Assets/Project/Art/Fonts/Galmuri11/Galmuri11 Pixel.asset";
        private const int UILayerIndex = 5;

        // 수첩 스프라이트(400px)를 3배 정수 배율로. 아래 좌표는 스프라이트 픽셀을 3배 해 중심 기준으로 옮긴 값
        private const float BookSize = 1200f;
        private static readonly Rect LeftPage = Rect.MinMaxRect(-470f, -350f, -30f, 340f);
        private static readonly Rect RightPage = Rect.MinMaxRect(60f, -350f, 490f, 340f);
        private static readonly Rect CoverArea = Rect.MinMaxRect(60f, -330f, 500f, 300f);
        private const float TabBarY = 400f;
        private static readonly Vector2 TabSize = new Vector2(150f, 52f);
        private const float TabSpacing = 8f;
        private static readonly Vector2 CloseButtonPosition = new Vector2(540f, 400f);
        private static readonly Vector2 CloseButtonSize = new Vector2(52f, 52f);
        private const float PageTitleHeight = 56f;
        private const float EntryHeight = 40f;
        private static readonly Vector2 StickyCell = new Vector2(200f, 200f);
        private const int StickyPadding = 28;

        private const float TitleFontSize = 30f;
        private const float BodyFontSize = 22f;
        private const float TabFontSize = 22f;

        // HUD 배치 (1920x1080 기준)
        private const float HudMargin = 32f;
        private static readonly Vector2 NotebookButtonSize = new Vector2(128f, 128f);
        private static readonly Vector2 BadgeSize = new Vector2(36f, 36f);
        private static readonly Vector2 QuestPanelSize = new Vector2(440f, 0f);
        private const int QuestPanelPadding = 12;
        private const float QuestLineSpacing = 4f;
        private static readonly Vector2 ToastSize = new Vector2(420f, 56f);
        private const float HudFontSize = 22f;

        // 중간 톤 크래프트 브라운 계열 (크림/보라 금지)
        private static readonly Color TextColor = new Color(0.23f, 0.17f, 0.16f, 1f);
        private static readonly Color KraftColor = new Color(0.66f, 0.51f, 0.36f, 1f);
        private static readonly Color KraftPanelColor = new Color(0.66f, 0.51f, 0.36f, 0.88f);
        private static readonly Color TabActiveColor = new Color(0.82f, 0.71f, 0.58f, 1f);
        private static readonly Color BadgeColor = new Color(0.64f, 0.25f, 0.17f, 1f);
        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color EntryHighlightColor = new Color(0.55f, 0.40f, 0.27f, 0.25f);

        private static readonly string[] TabKeys =
        {
            "@ui.notebook.tab.cover", "@ui.notebook.tab.quests", "@ui.notebook.tab.residents",
            "@ui.notebook.tab.alibis", "@ui.notebook.tab.questions", "@ui.notebook.tab.documents"
        };

        [MenuItem("LOTW/UI/Build Notebook UI")]
        public static void Run()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if(font == null)
                Debug.LogWarning($"[NotebookUIBuilder] Font not found: {FontPath}");

            NotebookUI notebook = BuildNotebookPrefab(font);
            SetupHud(font);
            RegisterPrefab(notebook);
            AssetDatabase.SaveAssets();
        }

        #region Notebook

        private static NotebookUI BuildNotebookPrefab(TMP_FontAsset font)
        {
            var existing = AssetDatabase.LoadAssetAtPath<NotebookUI>(NotebookPrefabPath);
            if(existing != null)
            {
                Debug.Log($"[NotebookUIBuilder] {NotebookPrefabPath} already exists, skipped. Delete it to rebuild.");
                return existing;
            }

            var rootGo = new GameObject("PF_NotebookUI", typeof(RectTransform), typeof(CanvasGroup));
            try
            {
                rootGo.layer = UILayerIndex;
                RectTransform root = (RectTransform)rootGo.transform;
                Stretch(root);

                Image dim = rootGo.AddComponent<Image>();
                dim.color = DimColor;

                RectTransform listRoot = CreateUIObject("ListRoot", root);
                Stretch(listRoot);
                CreateBook("Book", listRoot, InnerSpritePath);
                RectTransform left = CreateArea("LeftPage", listRoot, LeftPage);
                RectTransform right = CreateArea("RightPage", listRoot, RightPage);

                TMP_Text pageTitle = CreateText("PageTitle", left, font, TitleFontSize, TextAlignmentOptions.TopLeft);
                PlaceTop((RectTransform)pageTitle.transform, 0f, PageTitleHeight);
                RectTransform entries = CreateUIObject("Entries", left);
                Stretch(entries);
                entries.offsetMax = new Vector2(0f, -PageTitleHeight);
                var entryLayout = entries.gameObject.AddComponent<VerticalLayoutGroup>();
                entryLayout.childControlWidth = true;
                entryLayout.childControlHeight = false;
                entryLayout.childForceExpandWidth = true;
                entryLayout.childForceExpandHeight = false;
                Button entryTemplate = CreateEntryTemplate(entries, font);
                TMP_Text emptyText = CreateText("EmptyText", left, font, BodyFontSize, TextAlignmentOptions.Center);
                Stretch((RectTransform)emptyText.transform);

                TMP_Text detailTitle = CreateText("DetailTitle", right, font, TitleFontSize, TextAlignmentOptions.TopLeft);
                PlaceTop((RectTransform)detailTitle.transform, 0f, PageTitleHeight);
                TMP_Text detailBody = CreateText("DetailBody", right, font, BodyFontSize, TextAlignmentOptions.TopLeft);
                RectTransform bodyRect = (RectTransform)detailBody.transform;
                Stretch(bodyRect);
                bodyRect.offsetMax = new Vector2(0f, -PageTitleHeight);

                RectTransform coverRoot = CreateUIObject("CoverRoot", root);
                Stretch(coverRoot);
                CreateBook("Cover", coverRoot, CoverSpritePath);
                RectTransform stickies = CreateArea("Stickies", coverRoot, CoverArea);
                var grid = stickies.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = StickyCell;
                grid.childAlignment = TextAnchor.MiddleCenter;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 2;
                Sprite yellow = LoadSprite(YellowStickyPath);
                Sprite green = LoadSprite(GreenStickyPath);
                Image stickyTemplate = CreateStickyTemplate(stickies, yellow, font);

                Button[] tabs = CreateTabs(root, font);
                Button close = CreateTextButton("CloseButton", root, font, "X", CloseButtonSize);
                ((RectTransform)close.transform).anchoredPosition = CloseButtonPosition;

                NotebookUI ui = rootGo.AddComponent<NotebookUI>();
                var so = new SerializedObject(ui);
                SetArray(so, "tabButtons", tabs);
                Set(so, "coverRoot", coverRoot.gameObject);
                Set(so, "stickyContainer", stickies);
                Set(so, "stickyTemplate", stickyTemplate);
                Set(so, "mainStickySprite", yellow);
                Set(so, "subStickySprite", green);
                Set(so, "listRoot", listRoot.gameObject);
                Set(so, "pageTitle", pageTitle);
                Set(so, "entryContainer", entries);
                Set(so, "entryTemplate", entryTemplate);
                Set(so, "emptyText", emptyText);
                Set(so, "detailTitle", detailTitle);
                Set(so, "detailBody", detailBody);
                Set(so, "closeButton", close);
                so.ApplyModifiedPropertiesWithoutUndo();

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(rootGo, NotebookPrefabPath);
                return saved.GetComponent<NotebookUI>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootGo);
            }
        }

        private static void CreateBook(string name, RectTransform parent, string spritePath)
        {
            RectTransform book = CreateUIObject(name, parent);
            book.sizeDelta = new Vector2(BookSize, BookSize);
            Image image = book.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(spritePath);
            image.preserveAspect = true;
        }

        private static Button[] CreateTabs(RectTransform root, TMP_FontAsset font)
        {
            RectTransform bar = CreateUIObject("Tabs", root);
            bar.anchoredPosition = new Vector2(0f, TabBarY);
            bar.sizeDelta = new Vector2((TabSize.x + TabSpacing) * TabKeys.Length, TabSize.y);
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = TabSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var tabs = new Button[TabKeys.Length];
            for(int i = 0; i < TabKeys.Length; i++)
            {
                Button tab = CreateTextButton($"Tab_{(NotebookTab)i}", bar, font, string.Empty, TabSize);
                ColorBlock colors = tab.colors;
                colors.disabledColor = TabActiveColor;
                tab.colors = colors;
                tab.GetComponentInChildren<TMP_Text>().gameObject.AddComponent<LocalizedText>();
                var label = new SerializedObject(tab.GetComponentInChildren<LocalizedText>());
                label.FindProperty("key").stringValue = TabKeys[i];
                label.ApplyModifiedPropertiesWithoutUndo();
                tabs[i] = tab;
            }
            return tabs;
        }

        private static Button CreateEntryTemplate(RectTransform parent, TMP_FontAsset font)
        {
            RectTransform item = CreateUIObject("EntryTemplate", parent);
            item.sizeDelta = new Vector2(0f, EntryHeight);
            Image highlight = item.gameObject.AddComponent<Image>();
            var button = item.gameObject.AddComponent<Button>();
            button.targetGraphic = highlight;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.clear;
            colors.highlightedColor = EntryHighlightColor;
            colors.selectedColor = Color.clear;
            colors.pressedColor = EntryHighlightColor;
            colors.disabledColor = Color.clear;
            button.colors = colors;

            TMP_Text label = CreateText("Label", item, font, BodyFontSize, TextAlignmentOptions.MidlineLeft);
            Stretch((RectTransform)label.transform);
            label.raycastTarget = false;
            return button;
        }

        private static Image CreateStickyTemplate(RectTransform parent, Sprite sprite, TMP_FontAsset font)
        {
            RectTransform sticky = CreateUIObject("StickyTemplate", parent);
            Image image = sticky.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;

            TMP_Text label = CreateText("Label", sticky, font, BodyFontSize, TextAlignmentOptions.Center);
            RectTransform rect = (RectTransform)label.transform;
            Stretch(rect);
            rect.offsetMin = new Vector2(StickyPadding, StickyPadding);
            rect.offsetMax = new Vector2(-StickyPadding, -StickyPadding);
            label.raycastTarget = false;
            return image;
        }

        #endregion

        #region HUD

        private static void SetupHud(TMP_FontAsset font)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HudPrefabPath);
            try
            {
                var hud = root.GetComponent<HudUI>();
                if(hud == null)
                {
                    Debug.LogError("[NotebookUIBuilder] HudUI component missing on PF_HudUI root");
                    return;
                }

                RectTransform rootRect = (RectTransform)root.transform;
                Button notebookButton = FindOrCreate(rootRect, "NotebookButton", r => CreateNotebookButton(r, font)).GetComponent<Button>();
                Transform badge = notebookButton.transform.Find("UnreadBadge");
                RectTransform questPanel = FindOrCreate(rootRect, "QuestList", r => CreateQuestPanel(r, font));
                RectTransform toast = FindOrCreate(rootRect, "Toast", r => CreateToast(r, font));

                var so = new SerializedObject(hud);
                Set(so, "notebookButton", notebookButton);
                Set(so, "unreadBadge", badge != null ? badge.gameObject : null);
                Set(so, "unreadCountText", badge != null ? badge.GetComponentInChildren<TMP_Text>(true) : null);
                Set(so, "questListRoot", questPanel.gameObject);
                SetArray(so, "questLines", questPanel.GetComponentsInChildren<TMP_Text>(true));
                Set(so, "toastRoot", toast.gameObject);
                Set(so, "toastText", toast.GetComponentInChildren<TMP_Text>(true));
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static RectTransform CreateNotebookButton(RectTransform root, TMP_FontAsset font)
        {
            RectTransform rect = CreateUIObject("NotebookButton", root);
            Anchor(rect, Vector2.one);
            rect.anchoredPosition = new Vector2(-HudMargin, -HudMargin);
            rect.sizeDelta = NotebookButtonSize;
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(ButtonSpritePath);
            image.preserveAspect = true;
            rect.gameObject.AddComponent<Button>().targetGraphic = image;

            RectTransform badge = CreateUIObject("UnreadBadge", rect);
            Anchor(badge, Vector2.up);
            badge.sizeDelta = BadgeSize;
            badge.gameObject.AddComponent<Image>().color = BadgeColor;
            TMP_Text count = CreateText("Count", badge, font, HudFontSize, TextAlignmentOptions.Center);
            Stretch((RectTransform)count.transform);
            count.color = Color.white;
            count.raycastTarget = false;
            badge.gameObject.SetActive(false);
            return rect;
        }

        private static RectTransform CreateQuestPanel(RectTransform root, TMP_FontAsset font)
        {
            RectTransform panel = CreateUIObject("QuestList", root);
            Anchor(panel, Vector2.up);
            panel.anchoredPosition = new Vector2(HudMargin, -HudMargin);
            panel.sizeDelta = QuestPanelSize;
            Image bg = panel.gameObject.AddComponent<Image>();
            bg.color = KraftPanelColor;
            bg.raycastTarget = false;
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(QuestPanelPadding, QuestPanelPadding, QuestPanelPadding, QuestPanelPadding);
            layout.spacing = QuestLineSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for(int i = 0; i < UIDefines.HudQuestLines; i++)
            {
                TMP_Text text = CreateText($"Line_{i}", panel, font, HudFontSize, TextAlignmentOptions.MidlineLeft);
                text.raycastTarget = false;
                text.gameObject.SetActive(false);
            }
            panel.gameObject.SetActive(false);
            return panel;
        }

        private static RectTransform CreateToast(RectTransform root, TMP_FontAsset font)
        {
            RectTransform toast = CreateUIObject("Toast", root);
            Anchor(toast, new Vector2(0.5f, 1f));
            toast.anchoredPosition = new Vector2(0f, -HudMargin);
            toast.sizeDelta = ToastSize;
            Image bg = toast.gameObject.AddComponent<Image>();
            bg.color = KraftPanelColor;
            bg.raycastTarget = false;
            TMP_Text text = CreateText("Text", toast, font, HudFontSize, TextAlignmentOptions.Center);
            Stretch((RectTransform)text.transform);
            text.raycastTarget = false;
            toast.gameObject.SetActive(false);
            return toast;
        }

        #endregion

        #region Registration

        private static void RegisterPrefab(NotebookUI prefab)
        {
            if(prefab == null)
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(SubSystemPrefabPath);
            try
            {
                var manager = root.GetComponentInChildren<UIManager>(true);
                if(manager == null)
                {
                    Debug.LogError("[NotebookUIBuilder] UIManager not found in SubSystemCollection");
                    return;
                }

                var so = new SerializedObject(manager);
                SerializedProperty list = so.FindProperty("uiPrefabs");
                for(int i = 0; i < list.arraySize; i++)
                {
                    if(list.GetArrayElementAtIndex(i).objectReferenceValue == prefab)
                        return;
                }
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, SubSystemPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        #endregion

        #region Helpers

        private static RectTransform FindOrCreate(RectTransform root, string name, Func<RectTransform, RectTransform> create)
        {
            Transform found = root.Find(name);
            return found != null ? (RectTransform)found : create(root);
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(sprite == null)
                Debug.LogWarning($"[NotebookUIBuilder] Sprite not found: {path}");
            return sprite;
        }

        private static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UILayerIndex;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform CreateArea(string name, RectTransform parent, Rect area)
        {
            RectTransform rect = CreateUIObject(name, parent);
            rect.anchoredPosition = area.center;
            rect.sizeDelta = area.size;
            return rect;
        }

        private static TMP_Text CreateText(string name, RectTransform parent, TMP_FontAsset font, float size, TextAlignmentOptions alignment)
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

        private static Button CreateTextButton(string name, RectTransform parent, TMP_FontAsset font, string label, Vector2 size)
        {
            RectTransform rect = CreateUIObject(name, parent);
            rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = KraftColor;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            TMP_Text text = CreateText("Label", rect, font, TabFontSize, TextAlignmentOptions.Center);
            Stretch((RectTransform)text.transform);
            text.text = label;
            text.raycastTarget = false;
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void PlaceTop(RectTransform rect, float top, float height)
        {
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(0f, height);
        }

        private static void Anchor(RectTransform rect, Vector2 corner)
        {
            rect.anchorMin = corner;
            rect.anchorMax = corner;
            rect.pivot = corner;
        }

        private static void Set(SerializedObject so, string field, UnityEngine.Object value)
        {
            SerializedProperty prop = so.FindProperty(field);
            if(prop == null)
            {
                Debug.LogError($"[NotebookUIBuilder] Field not found: {so.targetObject.GetType().Name}.{field}");
                return;
            }
            prop.objectReferenceValue = value;
        }

        private static void SetArray<T>(SerializedObject so, string field, T[] values) where T : UnityEngine.Object
        {
            SerializedProperty prop = so.FindProperty(field);
            if(prop == null)
            {
                Debug.LogError($"[NotebookUIBuilder] Field not found: {so.targetObject.GetType().Name}.{field}");
                return;
            }
            prop.arraySize = values.Length;
            for(int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        #endregion
    }
}
