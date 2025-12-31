using UnityEngine;

namespace Project.Scripts.Data
{
    public readonly struct UIDefines
    {
        public static readonly int AnimShow = Animator.StringToHash("Show");
        public static readonly int AnimHide = Animator.StringToHash("Hide");
        public static readonly int AnimSelect = Animator.StringToHash("Select");
    }
}