// PF_DialogueUI 프리팹 꾸미기 (에디터 전용). 종이 대화창 배경과 3지선다 ChoicePanel을 만들고 DialogueUI 필드를 연결한다.
// 이미 TextBox/ChoicePanel이 있으면 새로 만들지 않고 연결만 다시 한다.
using Project.Scripts.Content.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Editor.Plaza
{
    public static class PlazaDialoguePrefabSetup
    {
        private const string PrefabPath = "Assets/Project/Prefabs/UI/PF_DialogueUI.prefab";
        private const string TextBoxSpritePath = "Assets/Project/Art/UI/TX_UI_TextBox.png";
        private const string ChoiceBoxSpritePath = "Assets/Project/Art/UI/TX_UI_ChoiceBox.png";
        private const string FontPath = "Assets/Project/Art/Fonts/Galmuri11/Galmuri11 Pixel.asset";
        private const string TextBoxName = "TextBox";
        private const string ChoicePanelName = "ChoicePanel";
        private const string ChoiceLabelName = "Label";
        private const int ChoiceCount = 3;
        private const int UILayer = 5;

        // 9-slice 테두리 (left, bottom, right, top). 오른쪽 아래 "ㄱ/" 표시가 늘어나지 않도록 오른쪽/아래를 넓게 둔다.
        private static readonly Vector4 TextBoxBorder = new Vector4(24f, 32f, 56f, 24f);

        // 1920x1080 기준 배치 (영상 f_001)
        private const float ScreenMarginX = 56f;
        private const float ScreenMarginBottom = 48f;
        private const float TextBoxHeight = 224f;
        private static readonly Vector2 SpeakerPosition = new Vector2(48f, -24f);
        private static readonly Vector2 SpeakerSize = new Vector2(400f, 40f);
        private static readonly Vector2 PromptOffsetMin = new Vector2(64f, 40f);
        private static readonly Vector2 PromptOffsetMax = new Vector2(-64f, -68f);
        private static readonly Vector2 ConfirmPosition = new Vector2(-260f, 24f);
        private static readonly Vector2 CancelPosition = new Vector2(-80f, 24f);
        private const float SpeakerFontSize = 22f;
        private const float PromptFontSize = 33f;

        private const float ChoiceMarginRight = 70f;
        private const float ChoiceGapAboveTextBox = 12f;
        private static readonly Vector2 ChoicePanelSize = new Vector2(300f, 226f);
        private const int ChoicePaddingLeft = 40;
        private const int ChoicePaddingRight = 56;
        private const int ChoicePaddingTop = 36;
        private const int ChoicePaddingBottom = 40;
        private const float ChoiceSpacing = 4f;
        private const float ChoiceFontSize = 22f;

        private static readonly Color TextColor = new Color(0.23f, 0.17f, 0.16f, 1f);
        private static readonly Color ChoiceNormalColor = new Color(1f, 1f, 1f, 0f);
        private static readonly Color ChoiceHighlightColor = new Color(1f, 1f, 1f, 0.9f);
        private static readonly Color ChoicePressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        [MenuItem("LOTW/Plaza/3 Setup Dialogue Prefab")]
        public static void Run()
        {
            Sprite textBoxSprite = LoadSprite(TextBoxSpritePath, TextBoxBorder);
            Sprite choiceBoxSprite = LoadSprite(ChoiceBoxSpritePath, Vector4.zero);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if(font == null)
                Debug.LogWarning($"[PlazaDialoguePrefabSetup] Font not found: {FontPath}");

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                SetupTextBox(root.transform, textBoxSprite, font);
                Transform panel = root.transform.Find(ChoicePanelName);
                if(panel == null)
                    panel = CreateChoicePanel(root.transform, choiceBoxSprite, font);
                WireDialogueUI(root, panel);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Sprite LoadSprite(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer == null)
            {
                Debug.LogError($"[PlazaDialoguePrefabSetup] Texture not found: {path}");
                return null;
            }

            if(border != Vector4.zero && importer.spriteBorder == Vector4.zero)
            {
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // 기존 텍스트/버튼을 화면 아래 종이 대화창 안으로 옮긴다. 이미 TextBox가 있으면 배경 스프라이트만 맞춘다.
        private static void SetupTextBox(Transform root, Sprite sprite, TMP_FontAsset font)
        {
            Transform existing = root.Find(TextBoxName);
            if(existing != null)
            {
                SetupImage(existing.gameObject, sprite, Image.Type.Sliced);
                return;
            }

            // 루트를 캔버스 전체로 늘려 화면 기준으로 배치
            var rootRect = (RectTransform)root;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            RectTransform box = CreateUIObject(TextBoxName, root);
            box.SetAsFirstSibling();
            box.anchorMin = Vector2.zero;
            box.anchorMax = new Vector2(1f, 0f);
            box.pivot = new Vector2(0.5f, 0f);
            box.offsetMin = new Vector2(ScreenMarginX, ScreenMarginBottom);
            box.offsetMax = new Vector2(-ScreenMarginX, ScreenMarginBottom + TextBoxHeight);
            SetupImage(box.gameObject, sprite, Image.Type.Sliced);

            RectTransform speaker = MoveInto(root, "SpeakerText", box);
            if(speaker != null)
            {
                speaker.anchorMin = Vector2.up;
                speaker.anchorMax = Vector2.up;
                speaker.pivot = Vector2.up;
                speaker.anchoredPosition = SpeakerPosition;
                speaker.sizeDelta = SpeakerSize;
                SetupText(speaker, font, SpeakerFontSize, TextAlignmentOptions.TopLeft);
            }

            RectTransform prompt = MoveInto(root, "PromptText", box);
            if(prompt != null)
            {
                prompt.anchorMin = Vector2.zero;
                prompt.anchorMax = Vector2.one;
                prompt.offsetMin = PromptOffsetMin;
                prompt.offsetMax = PromptOffsetMax;
                SetupText(prompt, font, PromptFontSize, TextAlignmentOptions.TopLeft);
            }

            PlaceBottomRight(MoveInto(root, "ConfirmButton", box), ConfirmPosition);
            PlaceBottomRight(MoveInto(root, "CancelButton", box), CancelPosition);
        }

        private static Transform CreateChoicePanel(Transform root, Sprite sprite, TMP_FontAsset font)
        {
            RectTransform panel = CreateUIObject(ChoicePanelName, root);
            panel.anchorMin = Vector2.right;
            panel.anchorMax = Vector2.right;
            panel.pivot = Vector2.right;
            panel.anchoredPosition = new Vector2(-ChoiceMarginRight, ScreenMarginBottom + TextBoxHeight + ChoiceGapAboveTextBox);
            panel.sizeDelta = ChoicePanelSize;
            SetupImage(panel.gameObject, sprite, Image.Type.Simple);

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(ChoicePaddingLeft, ChoicePaddingRight, ChoicePaddingTop, ChoicePaddingBottom);
            layout.spacing = ChoiceSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            for(int i = 0; i < ChoiceCount; i++)
            {
                RectTransform item = CreateUIObject($"Choice_{i}", panel);
                Image highlight = item.gameObject.AddComponent<Image>();
                var button = item.gameObject.AddComponent<Button>();
                button.targetGraphic = highlight;
                ColorBlock colors = button.colors;
                colors.normalColor = ChoiceNormalColor;
                colors.highlightedColor = ChoiceHighlightColor;
                colors.selectedColor = ChoiceHighlightColor;
                colors.pressedColor = ChoicePressedColor;
                button.colors = colors;

                RectTransform label = CreateUIObject(ChoiceLabelName, item);
                label.anchorMin = Vector2.zero;
                label.anchorMax = Vector2.one;
                label.offsetMin = Vector2.zero;
                label.offsetMax = Vector2.zero;
                var text = label.gameObject.AddComponent<TextMeshProUGUI>();
                text.raycastTarget = false;
                SetupText(label, font, ChoiceFontSize, TextAlignmentOptions.Center);
            }

            panel.gameObject.SetActive(false);
            return panel;
        }

        private static void WireDialogueUI(GameObject root, Transform panel)
        {
            var dialogueUI = root.GetComponent<DialogueUI>();
            if(dialogueUI == null)
            {
                Debug.LogError("[PlazaDialoguePrefabSetup] DialogueUI component missing on prefab root");
                return;
            }

            var serialized = new SerializedObject(dialogueUI);
            SerializedProperty panelProp = serialized.FindProperty("choicePanel");
            SerializedProperty buttonsProp = serialized.FindProperty("choiceButtons");
            SerializedProperty labelsProp = serialized.FindProperty("choiceLabels");
            if(panelProp == null || buttonsProp == null || labelsProp == null)
            {
                Debug.LogError("[PlazaDialoguePrefabSetup] DialogueUI has no choicePanel/choiceButtons/choiceLabels fields");
                return;
            }

            var buttons = panel.GetComponentsInChildren<Button>(true);
            panelProp.objectReferenceValue = panel.gameObject;
            buttonsProp.arraySize = buttons.Length;
            labelsProp.arraySize = buttons.Length;
            for(int i = 0; i < buttons.Length; i++)
            {
                buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
                labelsProp.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i].GetComponentInChildren<TMP_Text>(true);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UILayer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform MoveInto(Transform root, string childName, Transform parent)
        {
            Transform child = root.Find(childName);
            if(child == null)
            {
                Debug.LogWarning($"[PlazaDialoguePrefabSetup] {childName} not found in prefab");
                return null;
            }

            child.SetParent(parent, false);
            return (RectTransform)child;
        }

        private static void PlaceBottomRight(RectTransform rect, Vector2 position)
        {
            if(rect == null)
                return;

            rect.anchorMin = Vector2.right;
            rect.anchorMax = Vector2.right;
            rect.pivot = Vector2.right;
            rect.anchoredPosition = position;
        }

        private static void SetupImage(GameObject go, Sprite sprite, Image.Type type)
        {
            if(!go.TryGetComponent(out Image image))
                image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.preserveAspect = type == Image.Type.Simple;
        }

        private static void SetupText(RectTransform rect, TMP_FontAsset font, float size, TextAlignmentOptions alignment)
        {
            var text = rect.GetComponent<TMP_Text>();
            if(text == null)
                return;

            if(font != null)
                text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = TextColor;
        }
    }
}
