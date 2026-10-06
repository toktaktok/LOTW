using UnityEngine;

namespace Project.Scripts.Content.Minigame.GateDrop
{
    /// <summary>핀 하나의 연출: 캔이 닿으면 밝아지고 1픽셀 밀렸다가 돌아옵니다.</summary>
    public class GateDropPeg
    {
        private const float FlashDuration = 0.09f;
        private const float NudgeDuration = 0.07f;

        private readonly SpriteRenderer _renderer;
        private readonly Sprite _normal;
        private readonly Sprite _flash;
        private readonly GateDropLayout _layout;
        private readonly Vector2 _pixel;
        private float _flashTime;
        private float _nudgeTime;
        private Vector2 _nudge;

        public GateDropPeg(SpriteRenderer renderer, Sprite normal, Sprite flash, GateDropLayout layout, Vector2 pixel)
        {
            _renderer = renderer;
            _normal = normal;
            _flash = flash;
            _layout = layout;
            _pixel = pixel;
            renderer.transform.localPosition = layout.ToLocal(pixel);
        }

        /// <summary>캔이 닿음. direction은 캔이 튕겨 나가는 방향 (-1 왼쪽, 1 오른쪽). 핀은 반대쪽 아래로 밀림.</summary>
        public void Hit(int direction)
        {
            _flashTime = FlashDuration;
            _nudgeTime = NudgeDuration;
            _nudge = new Vector2(-direction, -1f);
            _renderer.sprite = _flash;
            _renderer.transform.localPosition = _layout.ToLocal(_pixel + _nudge);
        }

        public void Tick(float deltaTime)
        {
            if(_flashTime > 0f)
            {
                _flashTime -= deltaTime;
                if(_flashTime <= 0f)
                    _renderer.sprite = _normal;
            }
            if(_nudgeTime > 0f)
            {
                _nudgeTime -= deltaTime;
                if(_nudgeTime <= 0f)
                    _renderer.transform.localPosition = _layout.ToLocal(_pixel);
            }
        }
    }
}
