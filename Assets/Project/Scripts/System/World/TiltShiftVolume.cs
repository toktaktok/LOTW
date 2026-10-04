using UnityEngine;
using UnityEngine.Rendering;

namespace Project.Scripts.System.World
{
    /// <summary>
    /// 틸트시프트(TiltShift.shader) 설정. Volume 프로필에서 "LOTW/Tilt Shift"로 추가해 씬별로 조정합니다.
    /// 초점 띠 중심은 플레이어의 화면 높이를 따라가며(TiltShiftDriver), focusOffset만큼 위아래로 옮길 수 있습니다.
    /// maxRadius가 0이면 꺼집니다.
    /// </summary>
    [VolumeComponentMenu("LOTW/Tilt Shift")]
    public class TiltShiftVolume : VolumeComponent
    {
        [Tooltip("플레이어 화면 높이 기준 초점 띠 중심 오프셋 (화면 비율, +면 위)")]
        public ClampedFloatParameter focusOffset = new ClampedFloatParameter(0f, -0.5f, 0.5f);

        [Tooltip("완전히 선명한 띠의 절반 높이 (화면 비율)")]
        public ClampedFloatParameter focusWidth = new ClampedFloatParameter(0.2f, 0f, 0.5f);

        [Tooltip("띠 가장자리에서 최대 흐림까지의 거리 (화면 비율)")]
        public ClampedFloatParameter falloff = new ClampedFloatParameter(0.3f, 0.001f, 1f);

        [Tooltip("최대 흐림 반경 (저해상도 RT 픽셀). 0이면 꺼짐")]
        public ClampedFloatParameter maxRadius = new ClampedFloatParameter(1.5f, 0f, 16f);

        [Tooltip("흐림 샘플 수")]
        public ClampedIntParameter sampleCount = new ClampedIntParameter(12, 1, 32);

        [Tooltip("초점 띠 안의 채도 배율")]
        public ClampedFloatParameter focusSaturation = new ClampedFloatParameter(1f, 0f, 2f);
    }
}
