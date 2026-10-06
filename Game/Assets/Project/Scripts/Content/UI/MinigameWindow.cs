using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Project.Scripts.Core;
using Project.Scripts.Data;
using Project.Scripts.System.Minigame;
using Project.Scripts.System.World;
using Project.Scripts.Framework;
using Project.Scripts.Framework.Managers;
using Project.Scripts.Framework.UI;

namespace Project.Scripts.Content.UI
{
    /// <summary>
    /// 미니게임 창. 반투명 창틀 안에 스테이지 RT를 렌즈 머티리얼로 투사합니다.
    /// 열림: 창틀이 커진 뒤 화면이 사각 조리개처럼 열림. 닫힘은 그 반대.
    /// 창 크기는 스테이지 해상도 x 정수 배율, 위치는 정의의 layout을 따릅니다.
    /// </summary>
    public class MinigameWindow : BaseUI
    {
        [Header("UI References")]
        [Tooltip("창틀 루트. 크기/위치/열림 애니메이션 대상")]
        [SerializeField] private RectTransform window;
        [Tooltip("스테이지 RT를 표시하는 화면. window 안에서 여백만큼 안쪽으로 stretch")]
        [SerializeField] private RawImage screen;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button closeButton;

        [Header("Lens")]
        [Tooltip("정의에 screenMaterial이 없을 때 쓰는 기본 렌즈 머티리얼")]
        [SerializeField] private Material defaultScreenMaterial;

        private MinigameSession _session;
        private Material _screenMaterial;
        private Sequence _sequence;
        private Vector2 _openPosition;
        private Vector2 _targetPosition;
        // SourceBounds 배치: 월드 화면이 갱신될 때마다 발생원 영역을 따라감
        private bool _followSource;
        private Action _onOpened;
        private Action _onCloseRequested;
        private Action _onClosed;

        public RectTransform ScreenRect => screen.rectTransform;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(OnClose);
        }

        private void OnEnable() => LowResPixelRenderer.OnViewUpdated += OnWorldViewUpdated;
        private void OnDisable() => LowResPixelRenderer.OnViewUpdated -= OnWorldViewUpdated;

        private void OnDestroy() => ReleaseSession();

        // SourceBounds: 열려 있는 동안 카메라가 움직이거나 화면 배율이 바뀌어도 발생원을 따라감.
        // 월드 렌더러가 카메라를 픽셀 격자에 맞춘 직후에 계산해야 한 프레임 늦게 따라가지 않음
        private void OnWorldViewUpdated(LowResPixelRenderer view)
        {
            if(!_followSource || _session == null || view != LowResPixelRenderer.Current)
                return;
            if(!TryGetSourceBoundsLayout(((RectTransform)transform).rect.size, out Vector2 size, out Vector2 position))
                return;

            _targetPosition = position;
            _openPosition = position;
            window.anchoredPosition = position;
            if(SetScreenSize(size))
                FitHeader();
        }

        /// <summary>
        /// onOpened: 열림 애니메이션이 끝난 뒤. onCloseRequested: 닫기 버튼. onClosed: 창이 완전히 닫힌 뒤.
        /// </summary>
        public void Setup(MinigameSession session, RenderTexture stageTexture, Action onOpened, Action onCloseRequested, Action onClosed)
        {
            ReleaseSession();
            _session = session;
            _onOpened = onOpened;
            _onCloseRequested = onCloseRequested;
            _onClosed = onClosed;

            MinigameDefinition definition = session.Definition;
            // 조리개 값을 인스턴스에만 쓰도록 복제 (에셋 원본을 건드리지 않음)
            _screenMaterial = new Material(definition.ScreenMaterial != null ? definition.ScreenMaterial : defaultScreenMaterial);
            screen.texture = stageTexture;
            screen.material = _screenMaterial;

            titleText.text = Localization.Resolve(definition.Title);
            statusText.text = session.Status;
            session.OnStatusChanged += OnStatusChanged;
            closeButton.gameObject.SetActive(definition.AllowAbort);
        }

        public override async Awaitable ShowAsync()
        {
            if(IsVisible)
                return;

            currentUIState = UIState.Opening;
            gameObject.SetActive(true);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
            ApplyLayout();
            FitHeader();

            await PlayAsync(BuildOpenSequence());
            SetVisibility(true);
            _onOpened?.Invoke();
        }

        public override async Awaitable HideAsync()
        {
            if(currentUIState == UIState.Closing || currentUIState == UIState.Closed)
                return;

            currentUIState = UIState.Closing;
            canvasGroup.interactable = false;

            await PlayAsync(BuildCloseSequence());
            SetVisibility(false);

            Action onClosed = _onClosed;
            ReleaseSession();
            onClosed?.Invoke();
        }

        private void OnClose() => _onCloseRequested?.Invoke();

        private void OnStatusChanged(string status)
        {
            statusText.text = status;
            FitHeader();
        }

        private void ReleaseSession()
        {
            _sequence?.Kill();
            if(_session != null)
                _session.OnStatusChanged -= OnStatusChanged;
            _session = null;
            _followSource = false;
            _onOpened = null;
            _onCloseRequested = null;
            _onClosed = null;

            if(screen != null)
            {
                screen.texture = null;
                screen.material = null;
            }
            if(_screenMaterial != null)
                Destroy(_screenMaterial);
            _screenMaterial = null;
        }

        #region Layout

        // 창 크기 = 화면(스테이지 해상도 x 배율) + 창틀 여백. 위치는 화면 밖으로 나가지 않게 밀어 넣음.
        // SourceBounds는 화면을 발생원 영역에 그대로 겹치고, 밀어 넣지 않고 그 자리에서 열림.
        private void ApplyLayout()
        {
            MinigameDefinition definition = _session.Definition;
            MinigameWindowLayout layout = definition.Layout;
            Vector2 canvasSize = ((RectTransform)transform).rect.size;

            _followSource = false;
            if(layout.placement == MinigamePlacement.SourceBounds
                && TryGetSourceBoundsLayout(canvasSize, out Vector2 boundsSize, out Vector2 boundsPosition))
            {
                _followSource = true;
                SetScreenSize(boundsSize);
                _targetPosition = boundsPosition;
                _openPosition = boundsPosition;
                return;
            }

            int scale = layout.displayScale > 0 ? layout.displayScale : MinigameDefines.DefaultDisplayScale;
            SetScreenSize((Vector2)_session.StageResolution * scale);

            Vector2 sourcePosition = GetSourcePosition(canvasSize);
            Vector2 basePosition = layout.placement switch
            {
                MinigamePlacement.Anchor => (layout.anchor - new Vector2(0.5f, 0.5f)) * canvasSize,
                MinigamePlacement.Source => sourcePosition,
                _ => Vector2.zero,
            };

            _targetPosition = MinigameRules.ClampToScreen(basePosition + layout.offset, window.sizeDelta, canvasSize);
            _openPosition = layout.openFrom == MinigameOpenFrom.Source ? sourcePosition : _targetPosition;
        }

        // 미니게임을 연 오브젝트의 화면 위치 (캔버스 단위, 화면 중심 원점). 알 수 없으면 중심.
        private Vector2 GetSourcePosition(Vector2 canvasSize)
        {
            Camera worldCamera = Camera.main;
            if(_session.Source == null || worldCamera == null)
                return Vector2.zero;

            Vector3 viewport = worldCamera.WorldToViewportPoint(_session.Source.transform.position);
            return (new Vector2(viewport.x, viewport.y) - new Vector2(0.5f, 0.5f)) * canvasSize;
        }

        // 제목(왼쪽 정렬)과 상태(오른쪽 정렬)는 창 폭에 stretch. 둘이 겹치면 제목을 숨김.
        // 픽셀 글꼴이 흐려지지 않도록 글자 크기는 줄이지 않음.
        private void FitHeader()
        {
            float titleEnd = titleText.rectTransform.offsetMin.x + titleText.preferredWidth;
            float statusStart = window.rect.width + statusText.rectTransform.offsetMax.x - statusText.preferredWidth;
            titleText.enabled = string.IsNullOrEmpty(statusText.text) || titleEnd + MinigameDefines.HeaderGap <= statusStart;
        }

        // 화면은 window에 stretch로 붙어 있어 sizeDelta가 여백의 음수. 크기가 바뀌었으면 true
        private bool SetScreenSize(Vector2 screenSize)
        {
            Vector2 sizeDelta = screenSize - screen.rectTransform.sizeDelta;
            if(window.sizeDelta == sizeDelta)
                return false;

            window.sizeDelta = sizeDelta;
            return true;
        }

        // 발생원 영역(화면 픽셀)을 캔버스 단위로 바꿈. 창 위치는 화면(RawImage) 중심이 그 영역 중심에 오도록 창틀 오프셋을 뺌.
        private bool TryGetSourceBoundsLayout(Vector2 canvasSize, out Vector2 screenSize, out Vector2 windowPosition)
        {
            screenSize = default;
            windowPosition = default;
            if(!MinigameProjection.TryGetScreenRect(_session.Source, _session.StageResolution, out Rect screenRect))
                return false;

            Vector2 pixelSize = new Vector2(Screen.width, Screen.height);
            Vector2 canvasPerPixel = new Vector2(canvasSize.x / pixelSize.x, canvasSize.y / pixelSize.y);
            screenSize = Vector2.Scale(screenRect.size, canvasPerPixel);
            windowPosition = Vector2.Scale(screenRect.center - pixelSize * 0.5f, canvasPerPixel) - screen.rectTransform.anchoredPosition;
            return true;
        }

        #endregion

        #region Animation

        // 창틀 확장 -> 화면 조리개 열림
        private Sequence BuildOpenSequence()
        {
            window.localScale = Vector3.one * MinigameDefines.FrameStartScale;
            window.anchoredPosition = _openPosition;
            canvasGroup.alpha = 0f;
            SetIris(0f);

            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            sequence.Append(window.DOScale(1f, MinigameDefines.FrameOpenDuration).SetEase(Ease.OutQuad));
            sequence.Join(window.DOAnchorPos(_targetPosition, MinigameDefines.FrameOpenDuration).SetEase(Ease.OutQuad));
            sequence.Join(canvasGroup.DOFade(1f, MinigameDefines.FrameOpenDuration));
            sequence.Append(DOVirtual.Float(0f, 1f, MinigameDefines.IrisOpenDuration, SetIris).SetEase(Ease.OutQuad));
            return sequence;
        }

        // 조리개 닫힘 -> 창틀 축소
        private Sequence BuildCloseSequence()
        {
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            sequence.Append(DOVirtual.Float(1f, 0f, MinigameDefines.IrisCloseDuration, SetIris).SetEase(Ease.InQuad));
            sequence.Append(window.DOScale(MinigameDefines.FrameStartScale, MinigameDefines.FrameCloseDuration).SetEase(Ease.InQuad));
            sequence.Join(window.DOAnchorPos(_openPosition, MinigameDefines.FrameCloseDuration).SetEase(Ease.InQuad));
            sequence.Join(canvasGroup.DOFade(0f, MinigameDefines.FrameCloseDuration));
            return sequence;
        }

        // 가로세로가 같은 비율로 열리는 사각 조리개. 셰이더가 RT 픽셀 단위로 끊어 줌.
        private void SetIris(float open)
        {
            if(_screenMaterial == null)
                return;

            _screenMaterial.SetFloat(MinigameDefines.IrisXId, open);
            _screenMaterial.SetFloat(MinigameDefines.IrisYId, open);
        }

        // 시퀀스가 끝나거나 중간에 죽어도(오브젝트 파괴 등) 반드시 완료되어 UIManager 큐가 멈추지 않음
        private async Awaitable PlayAsync(Sequence sequence)
        {
            _sequence?.Kill();
            _sequence = sequence;

            AwaitableCompletionSource completion = new AwaitableCompletionSource();
            sequence.OnKill(() =>
            {
                // DOTween은 끝난 트윈을 재활용하므로 죽은 참조를 남기지 않음
                if(_sequence == sequence)
                    _sequence = null;
                completion.TrySetResult();
            });
            await completion.Awaitable;
        }

        #endregion
    }
}
