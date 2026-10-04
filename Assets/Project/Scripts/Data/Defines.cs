using UnityEngine;

namespace Project.Scripts.Data
{
    public readonly struct UIDefines
    {
        public const float DefaultFadeDuration = 0.3f;
        public const float ChoiceButtonSpacing = 4f;
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
        /// <summary>저해상도 RT 표시 캔버스. UIManager 레이어(0 이상)보다 아래.</summary>
        public const int LowResViewSortingOrder = -100;
        /// <summary>서브픽셀 보정용 저해상도 RT 가장자리 여백(픽셀, 한쪽 기준).</summary>
        public const int LowResMarginPixels = 1;
        /// <summary>화면 지우기 전용 카메라가 쓰는 렌더러(기능 없음). PC/Mobile RP 에셋 렌더러 목록의 같은 인덱스.</summary>
        public const int DisplayRendererIndex = 1;
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
    }
}