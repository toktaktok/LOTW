using UnityEngine;

namespace Project.Scripts.Content.Minigame.GateDrop
{
    /// <summary>출구 칸 하나: 음료 색 견본 캔과, 캔이 들어갔을 때 깜빡이며 빛나는 테두리.</summary>
    public class GateDropPocket
    {
        private const float SampleBrightness = 0.62f;
        private const float GlowDuration = 0.9f;
        private const float BlinkPeriod = 0.12f;

        private readonly SpriteRenderer _sample;
        private readonly SpriteRenderer _glow;
        private readonly Color _color;
        private float _glowTime;

        public GateDropPocket(SpriteRenderer sample, SpriteRenderer glow, Color color)
        {
            _sample = sample;
            _glow = glow;
            _color = color;
            Color dim = color * SampleBrightness;
            dim.a = 1f;
            sample.color = dim;
            glow.enabled = false;
        }

        public void HideSample() => _sample.enabled = false;

        public void Glow()
        {
            _glowTime = GlowDuration;
            _glow.enabled = true;
        }

        public void Tick(float deltaTime)
        {
            if(_glowTime <= 0f)
                return;

            _glowTime -= deltaTime;
            float elapsed = GlowDuration - _glowTime;
            // 처음 세 번은 깜빡이고, 그다음 천천히 꺼짐
            bool blinkOff = elapsed < BlinkPeriod * 6f && Mathf.FloorToInt(elapsed / BlinkPeriod) % 2 == 1;
            Color color = Color.Lerp(_color, Color.white, 0.35f);
            color.a = blinkOff ? 0.25f : Mathf.Clamp01(_glowTime / (GlowDuration * 0.5f));
            _glow.color = color;
            if(_glowTime <= 0f)
                _glow.enabled = false;
        }
    }
}
