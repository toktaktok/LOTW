using UnityEngine;

namespace Project.Scripts.Data
{
    public readonly struct UIDefines
    {
        public const float DefaultFadeDuration = 0.3f;
    }
    public readonly struct DialogueDefines
    {
        /// <summary>분기 행이 연달아 이어질 수 있는 최대 횟수. 분기 행끼리 순환하면 여기서 끊습니다.</summary>
        public const int MaxRouteDepth = 16;
    }
    public readonly struct AnimDefines
    {
        public static readonly int ShowID = Animator.StringToHash("Show");
        public static readonly int HideID = Animator.StringToHash("Hide");
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
    }

    public readonly struct CameraDefines
    {
        public const int DefaultCameraPriority = 10;
        public const int FirstCameraPriority = 20;

        /// <summary>캐릭터 스프라이트 기준 밀도(1유닛당 텍스처 픽셀 수).</summary>
        public const float PixelsPerUnit = 25f;
        /// <summary>복셀 한 칸이 차지하는 저해상도 RT 픽셀 수.</summary>
        public const int VoxelPixels = 5;
        /// <summary>복셀 한 칸의 월드 크기(= VoxelPixels / PixelsPerUnit). VoxelImporter importScale 기준.</summary>
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
        public const int DefaultDisplayScale = 4;
        public const int StageDepthBits = 16;

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
    }
}