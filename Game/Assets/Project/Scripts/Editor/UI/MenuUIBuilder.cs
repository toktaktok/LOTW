// 메인 화면/설정/일시정지/확인 팝업 프리팹과 Title 씬 생성 (에디터 전용).
// PF_TitleUI, PF_SettingsUI, PF_PauseUI, PF_ConfirmUI 를 만들고 uiPrefabs 에 등록, Title.unity 를 만들어 빌드 인덱스 0 에 둔다.
// 이미 있는 프리팹/씬은 새로 만들지 않는다. 씬은 추가 모드로 열어 저장 후 닫으므로 지금 열린 씬은 건드리지 않는다.
using System;
using System.Collections.Generic;
using System.Linq;
using Project.Scripts.Content.Title;
using Project.Scripts.Content.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Project.Scripts.Editor.UI.UIBuildUtil;

namespace Project.Scripts.Editor.UI
{
    public static class MenuUIBuilder
    {
        private const string TitlePrefabPath = "Assets/Project/Prefabs/UI/PF_TitleUI.prefab";
        private const string SettingsPrefabPath = "Assets/Project/Prefabs/UI/PF_SettingsUI.prefab";
        private const string PausePrefabPath = "Assets/Project/Prefabs/UI/PF_PauseUI.prefab";
        private const string ConfirmPrefabPath = "Assets/Project/Prefabs/UI/PF_ConfirmUI.prefab";
        private const string TitleScenePath = "Assets/Project/Scenes/Title.unity";
        private const string BriefcaseTexturePath = "Assets/Project/Art/UI/TX_UI_TitleBox_Open.png";
        private const string LogoSpritePath = "Assets/Project/Art/UI/TX_UI_Logo.png";

        private const string EventSystemType = "UnityEngine.EventSystems.EventSystem, UnityEngine.UI";
        private const string InputModuleType = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";

        // 서류가방 시트: 1500px 를 3x3 칸(500px)으로 나눈 8프레임 열림 애니메이션. 마지막(완전히 열림) 프레임을 2배로 표시
        private const int BriefcaseColumns = 3;
        private const int BriefcaseOpenFrame = 7;
        private static readonly Vector2 BriefcaseSize = new Vector2(1000f, 1000f);
        private static readonly Vector2 BriefcasePosition = new Vector2(40f, -120f);
        // 종이는 가방 뒤에 그려 가방 위로 나온 윗부분만 보인다 (가방 앞면 윗변 y = 가방 중심 + 140)
        private static readonly Vector2 SheetSize = new Vector2(150f, 300f);
        private static readonly float[] SheetX = { -120f, 30f, 180f };
        private const float SheetBottomY = -200f;
        private const float SheetLabelHeight = 60f;
        private static readonly Vector2 LogoSize = new Vector2(512f, 512f);
        private static readonly Vector2 LogoPosition = new Vector2(0f, 300f);
        private static readonly Vector2 ContinueInfoPosition = new Vector2(0f, -470f);
        private static readonly Vector2 ContinueInfoSize = new Vector2(600f, 48f);

        private static readonly Vector2 SettingsPanelSize = new Vector2(680f, 540f);
        private static readonly Vector2 PausePanelSize = new Vector2(440f, 420f);
        private static readonly Vector2 ConfirmPanelSize = new Vector2(680f, 300f);
        private static readonly Vector2 MenuButtonSize = new Vector2(320f, 64f);
        private static readonly Vector2 SmallButtonSize = new Vector2(200f, 56f);
        private static readonly Vector2 RowSize = new Vector2(560f, 56f);
        private const float RowLabelWidth = 200f;
        private const int PanelPadding = 32;
        private const float PanelSpacing = 16f;
        private const float HeaderFontSize = 32f;
        private const float BodyFontSize = 24f;

        private static readonly Color CameraBackground = new Color(0.22f, 0.17f, 0.14f, 1f);
        private static readonly Color SliderBackColor = new Color(0.45f, 0.34f, 0.25f, 1f);
        private static readonly Color SliderFillColor = new Color(0.36f, 0.24f, 0.16f, 1f);

        [MenuItem("LOTW/UI/Build Title And Menus")]
        public static void Run()
        {
            TMP_FontAsset font = LoadFont();
            var title = BuildPrefabOnce<TitleUI>(TitlePrefabPath, () => CreateTitle(font));
            var settings = BuildPrefabOnce<SettingsUI>(SettingsPrefabPath, () => CreateSettings(font));
            var pause = BuildPrefabOnce<PauseUI>(PausePrefabPath, () => CreatePause(font));
            var confirm = BuildPrefabOnce<ConfirmUI>(ConfirmPrefabPath, () => CreateConfirm(font));
            RegisterUIPrefabs(title, settings, pause, confirm);
            AssetDatabase.SaveAssets();

            CreateTitleScene();
            AddTitleToBuildSettings();
        }

        #region Prefabs

        private static GameObject CreateTitle(TMP_FontAsset font)
        {
            GameObject rootGo = CreateRoot("PF_TitleUI");
            RectTransform root = (RectTransform)rootGo.transform;

            RectTransform logo = CreateUIObject("Logo", root);
            logo.anchoredPosition = LogoPosition;
            logo.sizeDelta = LogoSize;
            Image logoImage = logo.gameObject.AddComponent<Image>();
            logoImage.sprite = LoadSprite(LogoSpritePath);
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;

            string[] keys = { "@ui.title.continue", "@ui.title.newgame", "@ui.title.settings" };
            var sheets = new TitleSheet[keys.Length];
            for(int i = 0; i < keys.Length; i++)
                sheets[i] = CreateSheet(root, font, keys[i], i);

            RectTransform briefcase = CreateUIObject("Briefcase", root);
            briefcase.anchoredPosition = BriefcasePosition;
            briefcase.sizeDelta = BriefcaseSize;
            RawImage caseImage = briefcase.gameObject.AddComponent<RawImage>();
            caseImage.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BriefcaseTexturePath);
            caseImage.uvRect = FrameRect(BriefcaseOpenFrame);
            caseImage.raycastTarget = false;

            TMP_Text info = CreateText("ContinueInfo", root, font, BodyFontSize, TextAlignmentOptions.Center);
            RectTransform infoRect = (RectTransform)info.transform;
            infoRect.anchoredPosition = ContinueInfoPosition;
            infoRect.sizeDelta = ContinueInfoSize;
            info.color = PaperColor;
            info.raycastTarget = false;

            // 키보드 이동은 가로 한 줄
            for(int i = 0; i < sheets.Length; i++)
            {
                Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnLeft = i > 0 ? sheets[i - 1].GetComponent<Button>() : null;
                nav.selectOnRight = i < sheets.Length - 1 ? sheets[i + 1].GetComponent<Button>() : null;
                sheets[i].GetComponent<Button>().navigation = nav;
            }

            var ui = rootGo.AddComponent<TitleUI>();
            var so = new SerializedObject(ui);
            Set(so, "continueSheet", sheets[0]);
            Set(so, "newGameSheet", sheets[1]);
            Set(so, "settingsSheet", sheets[2]);
            Set(so, "continueInfoText", info);
            so.ApplyModifiedPropertiesWithoutUndo();
            return rootGo;
        }

        private static TitleSheet CreateSheet(RectTransform root, TMP_FontAsset font, string key, int index)
        {
            RectTransform sheet = CreateUIObject($"Sheet_{index}", root);
            sheet.pivot = new Vector2(0.5f, 0f);
            sheet.anchoredPosition = BriefcasePosition + new Vector2(SheetX[index], SheetBottomY);
            sheet.sizeDelta = SheetSize;
            Image image = sheet.gameObject.AddComponent<Image>();
            image.color = PaperColor;
            var button = sheet.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            button.colors = colors;

            TMP_Text label = CreateLocalizedText("Label", sheet, font, BodyFontSize, TextAlignmentOptions.Center, key);
            PlaceTop((RectTransform)label.transform, 0f, SheetLabelHeight);
            return sheet.gameObject.AddComponent<TitleSheet>();
        }

        private static Rect FrameRect(int frame)
        {
            float cell = 1f / BriefcaseColumns;
            int column = frame % BriefcaseColumns;
            int row = frame / BriefcaseColumns;
            return new Rect(column * cell, 1f - (row + 1) * cell, cell, cell);
        }

        private static GameObject CreateSettings(TMP_FontAsset font)
        {
            GameObject rootGo = CreateRoot("PF_SettingsUI");
            RectTransform panel = CreateMenuPanel(rootGo, SettingsPanelSize, font, "@ui.settings.header");

            Slider master = CreateSliderRow(panel, font, "@ui.settings.master");
            Slider bgm = CreateSliderRow(panel, font, "@ui.settings.bgm");
            Slider sfx = CreateSliderRow(panel, font, "@ui.settings.sfx");
            RectTransform languageRow = CreateRow(panel, font, "@ui.settings.language");
            Button language = CreateTextButton("LanguageButton", languageRow, font, "@ui.language.name", SmallButtonSize);
            Button close = CreateTextButton("CloseButton", panel, font, "@ui.close", SmallButtonSize);

            var ui = rootGo.AddComponent<SettingsUI>();
            var so = new SerializedObject(ui);
            Set(so, "masterSlider", master);
            Set(so, "bgmSlider", bgm);
            Set(so, "sfxSlider", sfx);
            Set(so, "languageButton", language);
            Set(so, "closeButton", close);
            so.ApplyModifiedPropertiesWithoutUndo();
            return rootGo;
        }

        private static GameObject CreatePause(TMP_FontAsset font)
        {
            GameObject rootGo = CreateRoot("PF_PauseUI");
            RectTransform panel = CreateMenuPanel(rootGo, PausePanelSize, font, "@ui.pause.header");
            Button resume = CreateTextButton("ResumeButton", panel, font, "@ui.pause.resume", MenuButtonSize);
            Button settings = CreateTextButton("SettingsButton", panel, font, "@ui.pause.settings", MenuButtonSize);
            Button title = CreateTextButton("TitleButton", panel, font, "@ui.pause.title", MenuButtonSize);

            var ui = rootGo.AddComponent<PauseUI>();
            var so = new SerializedObject(ui);
            Set(so, "resumeButton", resume);
            Set(so, "settingsButton", settings);
            Set(so, "titleButton", title);
            so.ApplyModifiedPropertiesWithoutUndo();
            return rootGo;
        }

        private static GameObject CreateConfirm(TMP_FontAsset font)
        {
            GameObject rootGo = CreateRoot("PF_ConfirmUI");
            RectTransform panel = CreateMenuPanel(rootGo, ConfirmPanelSize, font, null);
            TMP_Text message = CreateText("Message", panel, font, BodyFontSize, TextAlignmentOptions.Center);
            ((RectTransform)message.transform).sizeDelta = new Vector2(RowSize.x, 140f);
            message.raycastTarget = false;

            RectTransform buttons = CreateUIObject("Buttons", panel);
            buttons.sizeDelta = new Vector2(RowSize.x, SmallButtonSize.y);
            var layout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = PanelSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            Button yes = CreateTextButton("YesButton", buttons, font, "@ui.confirm.yes", SmallButtonSize);
            Button no = CreateTextButton("NoButton", buttons, font, "@ui.confirm.no", SmallButtonSize);

            var ui = rootGo.AddComponent<ConfirmUI>();
            var so = new SerializedObject(ui);
            Set(so, "messageText", message);
            Set(so, "yesButton", yes);
            Set(so, "noButton", no);
            so.ApplyModifiedPropertiesWithoutUndo();
            return rootGo;
        }

        /// <summary>화면을 어둡게 덮고 가운데 크래프트 패널(세로 배치)을 둡니다. headerKey 가 null 이면 머리글 없음.</summary>
        private static RectTransform CreateMenuPanel(GameObject rootGo, Vector2 size, TMP_FontAsset font, string headerKey)
        {
            rootGo.AddComponent<Image>().color = DimColor;
            RectTransform panel = CreatePanel("Panel", rootGo.transform, size, KraftColor);
            VerticalLayoutGroup layout = AddVerticalLayout(panel, PanelSpacing, false);
            layout.padding = new RectOffset(PanelPadding, PanelPadding, PanelPadding, PanelPadding);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            if(headerKey != null)
            {
                TMP_Text header = CreateLocalizedText("Header", panel, font, HeaderFontSize, TextAlignmentOptions.Center, headerKey);
                ((RectTransform)header.transform).sizeDelta = new Vector2(RowSize.x, HeaderFontSize * 2f);
            }
            return panel;
        }

        private static RectTransform CreateRow(RectTransform parent, TMP_FontAsset font, string labelKey)
        {
            RectTransform row = CreateUIObject("Row", parent);
            row.sizeDelta = RowSize;
            TMP_Text label = CreateLocalizedText("Label", row, font, BodyFontSize, TextAlignmentOptions.MidlineLeft, labelKey);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(RowLabelWidth, 0f);
            return row;
        }

        private static Slider CreateSliderRow(RectTransform parent, TMP_FontAsset font, string labelKey)
        {
            RectTransform row = CreateRow(parent, font, labelKey);
            GameObject sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderGo.layer = UILayerIndex;
            foreach(Transform child in sliderGo.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = UILayerIndex;

            RectTransform rect = (RectTransform)sliderGo.transform;
            rect.SetParent(row, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(RowLabelWidth, -10f);
            rect.offsetMax = new Vector2(0f, 10f);

            var slider = sliderGo.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            Tint(sliderGo.transform, "Background", SliderBackColor);
            Tint(sliderGo.transform, "Fill Area/Fill", SliderFillColor);
            Tint(sliderGo.transform, "Handle Slide Area/Handle", TextColor);
            return slider;
        }

        private static void Tint(Transform root, string path, Color color)
        {
            Transform child = root.Find(path);
            if(child != null && child.TryGetComponent(out Image image))
                image.color = color;
        }

        #endregion

        #region Scene

        private static void CreateTitleScene()
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(TitleScenePath) != null)
            {
                Debug.Log($"[MenuUIBuilder] {TitleScenePath} already exists, skipped.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var cameraGo = new GameObject("Main Camera");
                cameraGo.tag = "MainCamera";
                var camera = cameraGo.AddComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = CameraBackground;
                cameraGo.AddComponent<AudioListener>();
                SceneManager.MoveGameObjectToScene(cameraGo, scene);

                var eventSystem = new GameObject("EventSystem");
                AddComponent(eventSystem, EventSystemType);
                AddComponent(eventSystem, InputModuleType);
                SceneManager.MoveGameObjectToScene(eventSystem, scene);

                var starter = new GameObject("[TitleScene]");
                starter.AddComponent<TitleScene>();
                SceneManager.MoveGameObjectToScene(starter, scene);

                EditorSceneManager.SaveScene(scene, TitleScenePath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AddComponent(GameObject go, string typeName)
        {
            Type type = Type.GetType(typeName);
            if(type == null)
            {
                Debug.LogError($"[MenuUIBuilder] Type not found: {typeName}");
                return;
            }
            go.AddComponent(type);
        }

        private static void AddTitleToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => s.path == TitleScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(TitleScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        #endregion
    }
}
