using UnityEngine;

namespace Project.Scripts.Content.Minigame.GateDrop
{
    /// <summary>
    /// 캔이 한 지점에서 다음 지점으로 튀어 가는 포물선 (순수 계산). 가로는 일정한 속도, 세로는 중력 포물선입니다.
    /// height만큼 위로 튀었다가 To에 내려앉습니다. height가 0이면 제자리에서 떨어지기 시작합니다.
    /// Lift는 From-To 직선(바닥 궤적)보다 위로 떠 있는 높이로, 그림자와 크기 연출에 씁니다.
    /// </summary>
    public struct GateDropHop
    {
        public Vector2 From;
        public Vector2 To;
        public float Height;
        public float Duration;
        public float Elapsed;

        public GateDropHop(Vector2 from, Vector2 to, float height, float duration)
        {
            From = from;
            To = to;
            Height = Mathf.Max(0f, height);
            Duration = Mathf.Max(0.01f, duration);
            Elapsed = 0f;
        }

        public float Progress => Mathf.Clamp01(Elapsed / Duration);
        public bool IsDone => Elapsed >= Duration;

        /// <summary>
        /// 포물선 y(t) = From.y + a t - b t^2 의 b. 꼭대기가 From.y + Height이고 t = 1에서 To.y.
        /// 직선에서 떠 있는 높이는 b t (1 - t).
        /// </summary>
        public float Curve
        {
            get
            {
                float drop = From.y - To.y;
                float h = Height;
                return drop + 2f * h + 2f * Mathf.Sqrt(Mathf.Max(0f, h * (drop + h)));
            }
        }

        public Vector2 Ground => Vector2.Lerp(From, To, Progress);

        public float Lift
        {
            get
            {
                float t = Progress;
                return Curve * t * (1f - t);
            }
        }

        public Vector2 Position => Ground + new Vector2(0f, Lift);
    }
}
