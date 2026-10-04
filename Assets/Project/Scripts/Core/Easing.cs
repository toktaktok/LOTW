namespace Project.Scripts.Core
{
    /// <summary>
    /// 트윈/페이드 보간에 사용하는 이징 곡선 모음입니다.
    /// 각 곡선은 정규화된 진행도 t(0~1)를 받아 보간 비율(0~1)을 반환하는 순수 함수입니다.
    /// UnityEngine 의존이 없어 EditMode 테스트에서 단독 검증이 가능합니다.
    /// 모든 곡선은 f(0)=0, f(1)=1 을 보장합니다.
    /// </summary>
    public enum EaseType
    {
        Linear,
        SmoothStep,
        QuadIn,
        QuadOut,
        QuadInOut,
        SineIn,
        SineOut,
        SineInOut
    }

    /// <summary>
    /// 정적 이징 함수 모음. BaseUI/AudioManager/SceneTransitionManager의
    /// SmoothStep+Lerp 중복을 대체합니다.
    /// </summary>
    public static class Easing
    {
        private const double Pi = 3.14159265358979323846;
        private const double HalfPi = Pi * 0.5;

        /// <summary>t를 [0,1] 범위로 제한합니다.</summary>
        public static float Clamp01(float t)
        {
            if (t < 0f) return 0f;
            if (t > 1f) return 1f;
            return t;
        }

        /// <summary>start와 end 사이를 t(0~1)로 선형 보간합니다. t는 클램프됩니다.</summary>
        public static float Lerp(float start, float end, float t)
        {
            return start + (end - start) * Clamp01(t);
        }

        /// <summary>EaseType에 해당하는 곡선으로 t(0~1)를 평가합니다. t는 클램프됩니다.</summary>
        public static float Evaluate(float t, EaseType type)
        {
            t = Clamp01(t);
            switch (type)
            {
                case EaseType.SmoothStep: return SmoothStep(t);
                case EaseType.QuadIn: return QuadIn(t);
                case EaseType.QuadOut: return QuadOut(t);
                case EaseType.QuadInOut: return QuadInOut(t);
                case EaseType.SineIn: return SineIn(t);
                case EaseType.SineOut: return SineOut(t);
                case EaseType.SineInOut: return SineInOut(t);
                case EaseType.Linear:
                default: return Linear(t);
            }
        }

        /// <summary>EaseType 곡선을 적용해 start~end 사이를 보간합니다. BaseUI/SceneTransition 페이드용.</summary>
        public static float Lerp(float start, float end, float t, EaseType type)
        {
            return start + (end - start) * Evaluate(t, type);
        }

        public static float Linear(float t) => Clamp01(t);

        /// <summary>3t^2 - 2t^3. Mathf.SmoothStep(0,1,t)와 동일한 곡선입니다.</summary>
        public static float SmoothStep(float t)
        {
            t = Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        public static float QuadIn(float t)
        {
            t = Clamp01(t);
            return t * t;
        }

        public static float QuadOut(float t)
        {
            t = Clamp01(t);
            return t * (2f - t);
        }

        public static float QuadInOut(float t)
        {
            t = Clamp01(t);
            if (t < 0.5f) return 2f * t * t;
            float u = -2f * t + 2f;
            return 1f - u * u * 0.5f;
        }

        public static float SineIn(float t)
        {
            t = Clamp01(t);
            return 1f - (float)System.Math.Cos(t * HalfPi);
        }

        public static float SineOut(float t)
        {
            t = Clamp01(t);
            return (float)System.Math.Sin(t * HalfPi);
        }

        public static float SineInOut(float t)
        {
            t = Clamp01(t);
            return -((float)System.Math.Cos(Pi * t) - 1f) * 0.5f;
        }
    }
}
