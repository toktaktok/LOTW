using UnityEngine;

namespace Project.Scripts.Content.Minigame.GateDrop
{
    /// <summary>
    /// 출구 위의 게이트. 문짝 두 개가 바깥 경첩을 축으로 아래로 열립니다.
    /// 열려 있는 동안 캔이 닿으면 그 레인 출구로 떨어지고, 닫혀 있으면 핀처럼 캔을 튕깁니다.
    /// 문짝 각도는 스프링이라 열고 닫을 때 지나쳤다가 돌아오고, 닫힌 채 맞으면 출렁입니다.
    /// </summary>
    public class GateDropGate
    {
        private const float OpenDegrees = 90f;
        private const float Stiffness = 900f;
        private const float Damping = 21f;
        private const float BumpVelocity = 7f;
        private const float FlashDuration = 0.08f;

        private readonly Transform _left;
        private readonly Transform _right;
        private readonly SpriteRenderer _leftRenderer;
        private readonly SpriteRenderer _rightRenderer;
        private readonly Color _color;
        private SpringValue _open;
        private float _openTimer;
        private float _flashTime;

        /// <summary>열림 시간이 남아 있는지 (닫히는 중이면 false).</summary>
        public bool IsOpen => _openTimer > 0f;

        public GateDropGate(SpriteRenderer left, SpriteRenderer right, Color color)
        {
            _left = left.transform;
            _right = right.transform;
            _leftRenderer = left;
            _rightRenderer = right;
            _color = color;
            _open = new SpringValue(0f);
            SetColor(color);
        }

        /// <summary>duration초 동안 엽니다. 이미 열려 있으면 시간을 다시 채웁니다.</summary>
        public void Open(float duration)
        {
            _openTimer = duration;
            _open.Target = 1f;
            _flashTime = FlashDuration;
        }

        public void Close()
        {
            _openTimer = 0f;
            _open.Target = 0f;
        }

        /// <summary>닫힌 게이트에 캔이 맞음: 문짝이 눌렸다가 튀어 오름.</summary>
        public void Bump()
        {
            _open.Velocity += BumpVelocity;
            _flashTime = FlashDuration;
        }

        /// <summary>열린 게이트로 캔이 빠져나감: 문짝이 한 번 더 밀려 흔들림.</summary>
        public void Pass()
        {
            _open.Velocity += BumpVelocity * 0.5f;
            _flashTime = FlashDuration;
        }

        public void Tick(float deltaTime)
        {
            if(_openTimer > 0f)
            {
                _openTimer -= deltaTime;
                if(_openTimer <= 0f)
                    _open.Target = 0f;
            }

            _open.Step(deltaTime, Stiffness, Damping);
            float degrees = _open.Value * OpenDegrees;
            _left.localRotation = Quaternion.Euler(0f, 0f, -degrees);
            _right.localRotation = Quaternion.Euler(0f, 0f, degrees);

            if(_flashTime > 0f)
            {
                _flashTime -= deltaTime;
                SetColor(_flashTime > 0f ? Color.Lerp(_color, Color.white, 0.75f) : _color);
            }
        }

        private void SetColor(Color color)
        {
            _leftRenderer.color = color;
            _rightRenderer.color = color;
        }
    }
}
