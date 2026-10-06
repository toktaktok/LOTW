using UnityEngine;
using Project.Scripts.Data;

namespace Project.Scripts.Content.Minigame.GateDrop
{
    /// <summary>
    /// 떨어지는 캔 (위에서 본 캔 윗면). 위치는 메카닉이 정하고, 이 클래스는 손맛 연출만 맡습니다.
    /// 부딪히면 납작해졌다가 출렁이며(스프링) 튕긴 방향으로 따개가 돌아갑니다. 찌그러짐은 미리 그린 타원 프레임으로 바꿔 픽셀이 깨지지 않습니다.
    /// 그림자는 바닥 궤적에 남아 캔이 튀어 오를수록 멀어지고 작아집니다.
    /// 착지하면 가운데부터 음료 색이 차오르고, 다 차면 캔 윗면 전체가 그 색이 됩니다.
    /// </summary>
    public class GateDropCan
    {
        private const float SquashStiffness = 650f;
        private const float SquashDamping = 13f;
        /// <summary>스프링 값이 이만큼 변할 때마다 찌그러짐 한 단계.</summary>
        private const float SquashPerLevel = 0.1f;
        private const float ImpactSpeed = 7f;
        private const float SpinSpeed = 520f;
        /// <summary>튕길 때 도는 속도가 이 비율만큼 무작위로 늘거나 줆.</summary>
        private const float SpinJitter = 0.2f;
        private const float SpinDecay = 2.5f;
        private const float FillDuration = 0.24f;
        /// <summary>차오름 곡선(easeOutBack)의 튀어나옴 세기와, 테두리까지 덮는 프레임으로 바꾸는 값(1 + 이 값 이상).</summary>
        private const float FillOvershoot = 1.70158f;
        private const float FillPopThreshold = 0.05f;
        /// <summary>다 찬 뒤 캔 윗면 색 = 음료 색을 흰색 쪽으로 이만큼 밝힘.</summary>
        private const float FilledBodyTint = 0.12f;
        private const float ShadowAlpha = 0.34f;
        private static readonly Vector2 ShadowOffset = new Vector2(1f, -2f);
        /// <summary>뜬 높이 1픽셀마다 그림자가 줄어드는 비율과 오른쪽 아래로 밀리는 픽셀.</summary>
        private const float ShadowShrinkPerPixel = 0.05f;
        private const float ShadowDrift = 0.25f;

        private readonly GateDropLayout _layout;
        private readonly Transform _root;
        private readonly SpriteRenderer _body;
        private readonly SpriteRenderer _fill;
        private readonly SpriteRenderer _shadow;
        private readonly GateDropArt _art;
        private SpringValue _squash;
        private float _spin;
        private float _spinVelocity;
        private float _fillTime = -1f;
        private Color _fillColor;

        public GateDropCan(GateDropLayout layout, Transform root, SpriteRenderer body, SpriteRenderer fill,
            SpriteRenderer shadow, GateDropArt art, Color color)
        {
            _layout = layout;
            _root = root;
            _body = body;
            _fill = fill;
            _shadow = shadow;
            _art = art;
            _squash = new SpringValue(0f);
            body.color = color;
            fill.enabled = false;
            shadow.color = new Color(0f, 0f, 0f, ShadowAlpha);
        }

        /// <summary>캔 중심(스테이지 픽셀)과 바닥 궤적 위로 떠 있는 높이.</summary>
        public void SetPosition(Vector2 pixel, float lift)
        {
            _root.localPosition = _layout.ToLocal(pixel);
            // 그림자는 바닥 궤적(캔 위치 - 떠 있는 높이)에 있고, 높이 뜰수록 오른쪽 아래로 멀어짐
            Vector2 shadow = new Vector2(0f, -lift) + ShadowOffset + new Vector2(lift * ShadowDrift, -lift * ShadowDrift);
            _shadow.transform.localPosition = new Vector3(Mathf.Round(shadow.x), Mathf.Round(shadow.y), 0f) / CameraDefines.PixelsPerUnit;
            // 크기는 미리 그린 홀수 지름 원으로 바꿈 (배율로 줄이면 픽셀이 깨짐)
            float shrink = Mathf.Clamp(1f - lift * ShadowShrinkPerPixel, GateDropArt.ShadowMinScale, 1f);
            _shadow.sprite = _art.GetShadow(shrink);
            _shadow.color = new Color(0f, 0f, 0f, ShadowAlpha * shrink);
        }

        /// <summary>부딪힘. direction: 튕겨 나가는 방향 (-1, 0, 1). strength: 1이 핀 한 번.</summary>
        public void Impact(int direction, float strength)
        {
            _squash.Velocity += ImpactSpeed * strength;
            if(direction != 0)
                _spinVelocity = direction * SpinSpeed * Random.Range(1f - SpinJitter, 1f + SpinJitter);
        }

        /// <summary>투입구에서 흔들리는 동안 따개만 살짝 돌림.</summary>
        public void Jiggle(float degrees) => _spin += degrees;

        public void Flood(Color color)
        {
            _fillColor = color;
            _fillTime = 0f;
            _fill.enabled = false;
            _fill.color = color;
        }

        public void Tick(float deltaTime)
        {
            _squash.Step(deltaTime, SquashStiffness, SquashDamping);
            int squash = Mathf.Clamp(Mathf.RoundToInt(_squash.Value / SquashPerLevel), -GateDropArt.SquashLevels, GateDropArt.SquashLevels);

            _spin += _spinVelocity * deltaTime;
            _spinVelocity *= Mathf.Exp(-SpinDecay * deltaTime);
            int frame = Mathf.RoundToInt(_spin / (360f / GateDropArt.CanFrameCount)) % GateDropArt.CanFrameCount;
            if(frame < 0)
                frame += GateDropArt.CanFrameCount;
            _body.sprite = _art.GetCan(squash, frame);

            TickFill(deltaTime, squash);
        }

        // 가운데에서 차오르다 테두리까지 한 번 부푼 뒤 맞춰지고, 다 차면 캔 윗면 자체를 음료 색으로 바꿈.
        // 크기는 미리 그린 단계 프레임으로 바꿈 (배율로 키우면 픽셀이 깨짐)
        private void TickFill(float deltaTime, int squash)
        {
            if(_fillTime < 0f)
                return;

            _fillTime += deltaTime;
            float t = Mathf.Clamp01(_fillTime / FillDuration);
            if(t >= 1f)
            {
                _body.color = Color.Lerp(_fillColor, Color.white, FilledBodyTint);
                _fill.enabled = false;
                _fillTime = -1f;
                return;
            }

            float eased = 1f + (FillOvershoot + 1f) * Mathf.Pow(t - 1f, 3f) + FillOvershoot * Mathf.Pow(t - 1f, 2f);
            int step = eased >= 1f + FillPopThreshold
                ? GateDropArt.FillSteps + 1
                : Mathf.Clamp(Mathf.CeilToInt(eased * GateDropArt.FillSteps), 0, GateDropArt.FillSteps);
            _fill.enabled = step > 0;
            if(step > 0)
                _fill.sprite = _art.GetFill(squash, step);
        }
    }
}
