using UnityEngine;

namespace Project.Scripts.Data
{
    public readonly struct UIDefines
    {
        public const float DefaultFadeDuration = 0.3f;
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
    }

    public readonly struct CameraDefines
    {
        public const int DefaultCameraPriority = 10;
        public const int FirstCameraPriority = 20;
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