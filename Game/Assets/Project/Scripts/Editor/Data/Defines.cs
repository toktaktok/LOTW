using UnityEngine;

namespace Project.Scripts.Editor.Data
{
    public readonly struct ToolDefines
    {
        // --- Path Tool ---
        // 에디터상 노드 크기
        public const float NodeGizmoRadius = 0.3f;
        // 에디터 자동 생성 시 노드 간격
        public const float NodeEditorSpacing = 2.0f;

        // --- Voxel DSS Applier ---
        public const string VoxelDssShaderName = "LOTW/VoxelDSS";
        // 일괄 적용 시 복셀 프리팹을 찾는 폴더
        public const string VoxelPrefabSearchFolder = "Assets/Project";
        // 새로 변환한 머티리얼의 toon 명암 단계 수
        public const float VoxelDssDefaultToonSteps = 3f;

        // --- Quest Mark Builder ---
        public const string NpcBasePrefabPath = "Assets/Project/Prefabs/Characters/PF_NPC_Base.prefab";
        // NPC 스프라이트 윗변과 땀 표시 사이 간격(유닛)
        public const float QuestMarkMargin = 0.3f;
        public static readonly string[] QuestMarkSpritePaths =
        {
            "Assets/Project/Art/UI/TX_UI_Sweat_1.png",
            "Assets/Project/Art/UI/TX_UI_Sweat_2.png"
        };

        // --- Plaza Builder ---
        public const string PlazaScenePath = "Assets/Project/Scenes/Plaza.unity";
        // 카메라 피치(도). yaw 0 고정
        public const float PlazaCameraPitch = 20f;
        public const float PlazaExploreOrthoSize = 10f;
        public const float PlazaDialogueOrthoSize = 7.2f;
        public const float PlazaCameraDistance = 20f;
        // 빌더가 매번 지우고 다시 만드는 루트 오브젝트 이름
        public const string PlazaGeneratedRootName = "--- Generated ---";
        // 복셀 프리팹 생성 시 VoxelObject importScale (기본값: 복셀 1칸 = RT 5픽셀. 건물별 값은 Set Voxel Pixels로 바꾼다)
        public const float VoxelImportScale = Project.Scripts.Data.CameraDefines.VoxelScale;
        public const string PlazaPrefabFolder = "Assets/Project/Prefabs/Environments";
        public const string PlazaVolumeProfilePath = "Assets/Project/Settings/Plaza_VolumeProfile.asset";
        public const string PlazaMaterialFolder = "Assets/Project/Art/Materials";
        // 복셀 모델(.vox) 루트. 끝에 / 포함
        public const string PlazaModelRoot = "Assets/Project/Art/Environments/Models/";
        // SnowMan 원본 계층이 있는 개발 씬
        public const string CharacterScenePath = "Assets/Project/Scenes/Test/Character.unity";

        // --- Dialogue Editor ---
        // Resources/ 기준 테이블 폴더와 파일 이름 (DataManager, Localization 과 같음)
        public const string DialogueTableFolder = "Table";
        public const string DialogueTableName = "Dialogue";
        public const string TextTablePrefix = "Text_";
        // 그래프를 여는 DataId 묶음 크기 (1100번대 = 대화 하나)
        public const int DialogueBlockSize = 100;
        // 스키마 Dialogue.ChoiceIds 의 maxCount 와 같음
        public const int DialogueMaxChoices = 4;
        // 툴이 만드는 행 전용 대사 키 접두사 (dialogue.{DataId})
        public const string DialogueTextKeyPrefix = "dialogue.";
        public const string DialogueTextTable = "Text_Dialogue";
        public const string DialogueNewLineText = "새 대사";
        // 선택지 칸에서 끌어 만든 행의 화자
        public const string DialoguePlayerSpeakerId = "player";
        public const string DialoguePlayerSpeakerName = "@character.player.name";
        // 프로젝트 폴더(Game/) 기준 Table 폴더. 엑셀 저장 스크립트와 노드 위치 파일이 있음
        public const string TableRootPath = "../Table";
        public const string TableEditScript = "table_edit.py";
        public const string TableConvertScript = "convert_table.py";
        public const string DialogueLayoutPath = "Layout/Dialogue.layout.json";
        // Temp/ 아래 저장 요청 파일
        public const string DialogueEditsTempPath = "Temp/DialogueEdits.json";
        public const string PythonPathPrefKey = "LOTW.PythonPath";
        // PATH 에 python 이 없을 때 찾아보는 설치 폴더 (%LOCALAPPDATA% 기준)
        public const string PythonInstallFolder = "Programs/Python";
        public const int PythonTimeoutMs = 60000;
        // 자동 배치 칸 크기
        public const float DialogueNodeColumnWidth = 320f;
        public const float DialogueNodeRowHeight = 180f;
        public const float DialogueNodeWidth = 260f;
        public const float DialogueNodePadding = 6f;
        // 시뮬레이션 중인 노드 테두리 두께
        public const float DialogueNodeBorderWidth = 3f;
        public const float DialogueSimPanelWidth = 340f;
        public const string DialogueItemTableName = "Item";
        public const float DialogueCommandNegateWidth = 40f;
        public const float DialogueCommandVerbWidth = 80f;
        public const float DialogueCommandPickWidth = 18f;
        public const float DialogueCommandOpWidth = 40f;
        public const float DialogueCommandValueWidth = 36f;
        public const float DialogueCommandRemoveWidth = 20f;
        public static readonly Color DialogueRouterColor = new Color(0.55f, 0.45f, 0.20f);
        public static readonly Color DialogueChoiceColor = new Color(0.20f, 0.40f, 0.55f);
        public static readonly Color DialogueEndColor = new Color(0.45f, 0.22f, 0.22f);
        public static readonly Color DialogueCurrentColor = new Color(0.95f, 0.80f, 0.25f);
        public static readonly Color DialogueBlockedColor = new Color(1f, 0.55f, 0.45f);

        // --- Editor Enhancers ---
        // 로컬 설정 파일 (UserSettings는 gitignore 대상)
        public const string EnhancerSettingsPath = "UserSettings/LOTW/EditorEnhancers.asset";
        public const string EnhancerPreferencesPath = "Preferences/LOTW/Editor Enhancers";
        // 이 접두사로 시작하는 오브젝트는 헤더(구분선)로 그림
        public const string HierarchyHeaderPrefix = "---";
        public const string HierarchyHeaderTag = "EditorOnly";
        // Hierarchy 들여쓰기 한 단계 폭
        public const float HierarchyIndent = 14f;
        public const float HierarchyIconSize = 16f;
        public const string HierarchyMissingScriptIcon = "console.warnicon.sml";
        // 마우스 이동 이벤트를 켜 줄 내장 Hierarchy 창 타입 이름
        public const string HierarchyWindowTypeName = "SceneHierarchyWindow";
        // 설정 창에서 규칙 추가 시 기본 이름
        public const string HierarchyRuleDefaultName = "Name*";
        public const string FolderRuleDefaultName = "NewFolder";
        // 색상 그라데이션 시작(왼쪽) 알파. 오른쪽 끝은 0
        public const float HierarchyBackgroundAlpha = 0.45f;
        public const float HierarchyHeaderGradientAlpha = 0.7f;
        public const int HierarchyGradientSteps = 32;
        // 아이콘 왼쪽 끝에서 이름 텍스트 시작까지 거리
        public const float HierarchyLabelOffset = 18f;
        // 비활성 오브젝트의 아이콘/이름 투명도
        public const float HierarchyInactiveAlpha = 0.5f;
        public static readonly Color HierarchyPrefabTextColor = new Color(0.48f, 0.67f, 0.95f);
        public static readonly Color HierarchyPrefabTextColorLight = new Color(0.05f, 0.27f, 0.62f);
        public static readonly Color HierarchyMissingPrefabTextColor = new Color(1f, 0.42f, 0.42f);
        public static readonly Color HierarchyMissingPrefabTextColorLight = new Color(0.7f, 0.1f, 0.1f);
        public static readonly Color HierarchyTreeLineColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        public static readonly Color HierarchyHeaderColor = new Color(0.16f, 0.16f, 0.16f, 1f);
        public static readonly Color HierarchyHeaderColorLight = new Color(0.62f, 0.62f, 0.62f, 1f);
        // 커스텀 아이콘 아래 기본 아이콘을 가리는 행 배경색 (에디터 기본값과 동일)
        public static readonly Color HierarchyRowColor = new Color(0.22f, 0.22f, 0.22f);
        public static readonly Color HierarchyRowColorLight = new Color(0.78f, 0.78f, 0.78f);
        public static readonly Color HierarchySelectedColor = new Color(0.17f, 0.36f, 0.53f);
        public static readonly Color HierarchySelectedColorLight = new Color(0.23f, 0.45f, 0.69f);
        // 폴더 위에 겹쳐 그리는 커스텀 아이콘 비율
        public const float FolderBadgeScale = 0.6f;
        // 스타일 선택 창
        public const float PickerSwatchSize = 20f;
        public const int PickerIconColumns = 8;
        public static readonly Vector2 PickerWindowSize = new Vector2(240f, 300f);

        public static readonly Color[] EnhancerPalette =
        {
            new Color(0.90f, 0.30f, 0.30f), // red
            new Color(0.95f, 0.55f, 0.20f), // orange
            new Color(0.95f, 0.80f, 0.25f), // yellow
            new Color(0.45f, 0.80f, 0.35f), // green
            new Color(0.25f, 0.75f, 0.70f), // teal
            new Color(0.30f, 0.65f, 0.95f), // sky
            new Color(0.25f, 0.40f, 0.85f), // blue
            new Color(0.60f, 0.45f, 0.30f), // brown
            new Color(0.60f, 0.60f, 0.60f), // gray
        };

        // 스타일 선택 창에 나열되는 내장 아이콘 (pro 스킨이면 d_ 변형 사용)
        public static readonly string[] EnhancerIcons =
        {
            "Prefab Icon", "GameObject Icon", "SceneAsset Icon", "cs Script Icon",
            "ScriptableObject Icon", "TextAsset Icon", "Material Icon", "Texture Icon",
            "Mesh Icon", "Shader Icon", "Font Icon", "AudioClip Icon",
            "AnimationClip Icon", "AnimatorController Icon", "Animator Icon", "Camera Icon",
            "Light Icon", "AudioSource Icon", "Canvas Icon", "EventSystem Icon",
            "ParticleSystem Icon", "Rigidbody Icon", "BoxCollider Icon", "Terrain Icon",
            "NavMeshAgent Icon", "GameManager Icon", "Favorite Icon", "console.warnicon.sml",
        };
    }
}
