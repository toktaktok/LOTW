using UnityEngine;

namespace Project.Scripts.Content.Minigame.GateDrop
{
    /// <summary>
    /// 감쇠 스프링으로 움직이는 값. 목표를 넘었다가 돌아오는 반동(쫀득한 움직임)에 씁니다.
    /// 프레임이 길어도 튀지 않도록 작은 단계로 나눠 적분합니다.
    /// </summary>
    public struct SpringValue
    {
        private const float MaxStep = 1f / 120f;

        public float Value;
        public float Velocity;
        public float Target;

        public SpringValue(float value)
        {
            Value = value;
            Velocity = 0f;
            Target = value;
        }

        public void Step(float deltaTime, float stiffness, float damping)
        {
            while(deltaTime > 0f)
            {
                float step = Mathf.Min(deltaTime, MaxStep);
                float acceleration = stiffness * (Target - Value) - damping * Velocity;
                Velocity += acceleration * step;
                Value += Velocity * step;
                deltaTime -= step;
            }
        }
    }
}
