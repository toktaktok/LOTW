using UnityEngine;

namespace Project.Scripts.Data
{
    public readonly struct UIDefines
    {
        public static readonly int AnimShow = Animator.StringToHash("Show");
        public static readonly int AnimHide = Animator.StringToHash("Hide");
        public static readonly int AnimSelect = Animator.StringToHash("Select");
    }

        public readonly struct WorldDefines
        {
            public static readonly float InteractionDistance = 30f;
            
            public static readonly float DefaultMoveSpeed = 5f;
            public static readonly float DefaultCorrectionSpeed = 10.0f;
            
            public const float InputThreshold = 0.01f; // 입력으로 간주할 최소값 (Deadzone)
    
            public const float NodeArrivalThreshold = 2f;    // 노드에 도달했다고 판단하는 거리
            public const float DirectionReversalThreshold = -0.1f; // 입력 방향이 반대라고 판단하는 내적값 기준
            public const float RailCorrectionDeadzone = 0.05f; // 이 거리 이내의 오차는 무시 (떨림 방지)
        }
}