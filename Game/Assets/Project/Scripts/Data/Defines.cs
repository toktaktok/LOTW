using UnityEngine;
using Project.Scripts.Framework.Managers;

namespace Project.Scripts.Data
{
    public readonly struct UIDefines
    {
        public const float DefaultFadeDuration = 0.3f;
        /// <summary>HUD 의뢰 목록 줄 수 (메인 고정 + 서브).</summary>
        public const int HudQuestLines = 3;
        /// <summary>"수첩에 기록됨" 알림 표시 시간(초, unscaled).</summary>
        public const float ToastDuration = 2.5f;
        /// <summary>HUD 키 표시에 쓰는 입력 컨트롤 스킴 (InputSystem_Actions).</summary>
        public const string KeyboardControlScheme = "Keyboard&Mouse";
        /// <summary>수첩 표지에 붙이는 포스트잇 최대 수.</summary>
        public const int MaxStickyNotes = 4;
    }
    public readonly struct SaveDefines
    {
        /// <summary>SaveData 형식 버전. 필드 의미가 바뀌면 올리고 SaveManager 에서 변환합니다.</summary>
        public const int Version = 1;
        public const int MaxSlots = 3;
    }
    public readonly struct DialogueDefines
    {
        /// <summary>분기 행이 연달아 이어질 수 있는 최대 횟수. 분기 행끼리 순환하면 여기서 끊습니다.</summary>
        public const int MaxRouteDepth = 16;
    }
    public readonly struct AnimDefines
    {
        public static readonly int SelectID = Animator.StringToHash("Select");
        public static readonly int IdleID = Animator.StringToHash("Idle");
        public static readonly int MoveID = Animator.StringToHash("Move");
    }
    public readonly struct WorldDefines
    {
        public const float InteractionDistance = 2f;
        
        public const float DefaultMoveSpeed = 5f;

        public const float InputThreshold = 0.01f;
        public const float DirectionReversalThreshold = -0.1f;
        public const float FacingThreshold = 0.01f;

        public const float MoveAndSwitchTimeout = 10f;
        /// <summary>걷기 이동(RailConnector)에서 목적 노드 도착으로 보는 수평 거리.</summary>
        public const float RailArrivalDistance = 0.1f;
        /// <summary>NPC 머리 위 의뢰 표시(땀) 프레임 간격(초).</summary>
        public const float QuestMarkFrameInterval = 0.4f;
    }

    public readonly struct CameraDefines
    {
        public const int DefaultCameraPriority = 10;
        public const int FirstCameraPriority = 20;

        /// <summary>캐릭터 스프라이트 기준 밀도(1유닛당 텍스처 픽셀 수).</summary>
        public const float PixelsPerUnit = 25f;
        /// <summary>새 복셀 프리팹의 기본 밀도: 복셀 한 칸이 차지하는 저해상도 RT 픽셀 수. 건물별 값은 프리팹 importScale이 정한다.</summary>
        public const int VoxelPixels = 5;
        /// <summary>기본 복셀 한 칸의 월드 크기(= VoxelPixels / PixelsPerUnit). VoxelImporter importScale 기준.</summary>
        public const float VoxelScale = VoxelPixels / PixelsPerUnit;
        /// <summary>저해상도 RT 표시 캔버스. UIManager 레이어(0 이상)보다 아래.</summary>
        public const int LowResViewSortingOrder = -100;
        /// <summary>서브픽셀 보정용 저해상도 RT 가장자리 여백(픽셀, 한쪽 기준).</summary>
        public const int LowResMarginPixels = 1;
        /// <summary>화면 지우기 전용 카메라가 쓰는 렌더러(기능 없음). PC/Mobile RP 에셋 렌더러 목록의 같은 인덱스.</summary>
        public const int DisplayRendererIndex = 1;
        /// <summary>NPC 대화 카메라 전환 블렌드 시간(초).</summary>
        public const float DialogueBlendDuration = 0.5f;
    }

    public readonly struct MinigameDefines
    {
        /// <summary>스테이지를 놓는 위치. 월드 카메라에 잡히지 않도록 멀리 둠.</summary>
        public static readonly Vector3 StageOrigin = new Vector3(0f, -1000f, 0f);
        public static readonly Vector2Int DefaultStageResolution = new Vector2Int(240, 150);
        /// <summary>SourceBounds 배치에서 발생원 영역이 이보다 작으면 이 크기로 늘림 (정의가 더 크게 정할 수 있음).</summary>
        public static readonly Vector2Int MinStageResolution = new Vector2Int(48, 48);
        /// <summary>투영 크기를 정수로 올릴 때 무시하는 오차(RT 픽셀). 104.0002가 105가 되지 않게.</summary>
        public const float ProjectionSizeTolerance = 0.01f;
        public const int DefaultDisplayScale = 4;
        public const int StageDepthBits = 16;
        /// <summary>창 제목과 상태 글자 사이 최소 간격(캔버스 단위). 이보다 좁으면 제목을 숨김.</summary>
        public const float HeaderGap = 8f;

        /// <summary>메카닉이 항상 읽을 수 있는 경과 시간 변수(초, 내림).</summary>
        public const string TimeVar = "time";
        public const string AbortedOutcomeName = "aborted";
        public const string FlagPrefix = "mg_";
        public const string PlaysFlagSuffix = "_plays";
        public const string ClearedFlagSuffix = "_cleared";

        /// <summary>결과가 확정된 뒤 창을 닫기 전까지 화면을 보여 주는 시간(초).</summary>
        public const float ResultHoldDuration = 0.6f;

        public const float FrameOpenDuration = 0.14f;
        public const float IrisOpenDuration = 0.32f;
        public const float IrisCloseDuration = 0.18f;
        public const float FrameCloseDuration = 0.12f;
        public const float FrameStartScale = 0.2f;

        public static readonly int IrisXId = Shader.PropertyToID("_IrisX");
        public static readonly int IrisYId = Shader.PropertyToID("_IrisY");
    }

    /// <summary>낙하 게이트 미니게임 판 배치 상수. 길이는 스테이지 픽셀.</summary>
    public readonly struct GateDropDefines
    {
        public const int MinOutlets = 2;
        public const int MaxOutlets = 8;
        /// <summary>레인(출구 1칸) 폭. 짝수만 씀.</summary>
        public const int MinLaneWidth = 10;
        public const int MaxLaneWidth = 24;
        public const int MinStageHeight = 96;
        /// <summary>판 바깥 테두리 두께.</summary>
        public const int Border = 2;
        public const int ButtonAreaHeight = 12;
        public const int ButtonHeight = 7;
        /// <summary>출구 칸 안에서 캔 위아래 여백.</summary>
        public const int OutletPadding = 2;
        /// <summary>투입구 위 여백.</summary>
        public const int InletPadding = 3;
        /// <summary>투입구 캔과 첫 줄 핀 사이 여백.</summary>
        public const int InletDropGap = 4;
        /// <summary>핀 줄 간격 목표 = 레인 폭 x 이 값.</summary>
        public const float RowStepPerLane = 0.72f;
        /// <summary>핀 줄 간격 최소 = 레인 폭 x 이 값. 반 레인(옆으로 가는 거리)만큼은 내려가 45도보다 눕지 않게.</summary>
        public const float MinRowStepPerLane = 0.5f;
        public const int MinRows = 3;
        public const int MaxRows = 31;
        public const int MinCanDiameter = 7;
        public const int MaxCanDiameter = 15;
        /// <summary>핀 크기 (홀수).</summary>
        public const int PegSize = 3;
        public const int GateThickness = 2;
        /// <summary>게이트 줄과 마지막 줄 사이 = 캔 지름 + 이 값. 마지막 줄 핀 위 캔이 문짝과 겹치지 않게.</summary>
        public const int GateRowGap = 3;
    }

    /// <summary>플레이 모드 맵 에디터 상수.</summary>
    public readonly struct MapDefines
    {
        public const float GroundPlaneY = 0f;
        public const float PlacementRayMaxDistance = 100f;
        public const float NodePickRadius = 0.5f;
        public const float DefaultNodeRadius = 0.3f;
    }

    /// <summary>
    /// 씬 이름 상수. 씬을 추가할 때마다 여기에 등록합니다.
    /// 사용 예: SceneTransitionManager.Instance.TransitionTo(SceneDefines.FlowerShop, "FromStreet");
    /// </summary>
    public readonly struct SceneDefines
    {
        public const string Character = "Character";
        public const string SetUp = "SetUp";
        public const string Plaza = "Plaza";
        /// <summary>메인 화면 (빌드 인덱스 0).</summary>
        public const string Title = "Title";
    }
    public readonly struct StoryDefines
    {
        /// <summary>새 게임에서 재생하는 프롤로그 시퀀스 (Sequence 테이블 첫 스텝 dataId). 첫 스텝이 씬 이동.</summary>
        public const int PrologueSequenceId = 1;
        /// <summary>시퀀스 페이드 스텝의 duration 이 0 일 때 쓰는 시간(초).</summary>
        public const float SequenceFadeDuration = 1f;
        /// <summary>한 시퀀스가 실행할 수 있는 최대 스텝 수. nextId 가 순환하면 여기서 끊습니다.</summary>
        public const int MaxSequenceSteps = 256;
    }
    public readonly struct MenuDefines
    {
        /// <summary>타이틀 서류가방에서 선택된 종이가 올라오는 높이(px, 1920x1080 기준).</summary>
        public const float SheetRaise = 24f;
        /// <summary>종이 올라옴/내려감 보간 속도 (unscaled, 초당).</summary>
        public const float SheetRaiseSpeed = 12f;
        /// <summary>설정에서 고를 수 있는 언어. Text 테이블 열 이름과 같음.</summary>
        public static readonly string[] Languages = { "ko", "en" };
    }
}