using UnityEngine;

namespace Project.Scripts.Data
{
    public readonly struct UIDefines
    {
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
        public const float DefaultCorrectionSpeed = 10.0f;
        
        public const float InputThreshold = 0.01f;

        public const float DirectionReversalThreshold = -0.1f;
        public const float RailCorrectionDeadzone = 0.05f;
    }

    public readonly struct CameraDefines
    {
        public const int DefaultCameraPriority = 10;
        public const int FirstCameraPriority = 20;
    }
}