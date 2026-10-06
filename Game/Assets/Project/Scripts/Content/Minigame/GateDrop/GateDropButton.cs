using UnityEngine;

namespace Project.Scripts.Content.Minigame.GateDrop
{
    /// <summary>음료 버튼 하나. 누르면 2픽셀 들어갔다가 스프링으로 튀어나오고 잠깐 밝아집니다.</summary>
    public class GateDropButton
    {
        private const float PressDepth = 2f;
        private const float Stiffness = 700f;
        private const float Damping = 20f;
        private const float FlashDuration = 0.16f;

        private readonly SpriteRenderer _renderer;
        private readonly GateDropLayout _layout;
        private readonly Vector2 _pixel;
        private readonly Color _color;
        private SpringValue _offset;
        private float _flashTime;

        public GateDropButton(SpriteRenderer renderer, GateDropLayout layout, Vector2 pixel, Color color)
        {
            _renderer = renderer;
            _layout = layout;
            _pixel = pixel;
            _color = color;
            _offset = new SpringValue(0f);
            renderer.color = color;
            renderer.transform.localPosition = layout.ToLocal(pixel);
        }

        public void Press()
        {
            _offset.Value = -PressDepth;
            _offset.Velocity = 0f;
            _flashTime = FlashDuration;
        }

        public void Tick(float deltaTime)
        {
            _offset.Step(deltaTime, Stiffness, Damping);
            _renderer.transform.localPosition = _layout.ToLocal(_pixel + new Vector2(0f, _offset.Value));

            if(_flashTime > 0f)
            {
                _flashTime = Mathf.Max(0f, _flashTime - deltaTime);
                _renderer.color = Color.Lerp(_color, Color.white, _flashTime / FlashDuration * 0.8f);
            }
        }
    }
}
