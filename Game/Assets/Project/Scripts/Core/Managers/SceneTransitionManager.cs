using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Project.Scripts.System.World;
using Project.Scripts.Content.Controller;

namespace Project.Scripts.Core.Managers
{
    /// <summary>
    /// 씬 전환 시 호출되는 콜백.
    /// 프로젝트별 로직(플레이어 배치, 카메라 세팅 등)을 주입합니다.
    /// </summary>
    public interface ISceneTransitionHandler
    {
        void OnBeforeTransition();
        void OnSceneLoaded(string sceneName, string entranceId);
        void OnAfterTransition();
    }

    /// <summary>
    /// 기본 핸들러: PlayerController를 SceneEntrance 위치에 배치하고 카메라 입력을 제어합니다.
    /// </summary>
    public class DefaultSceneTransitionHandler : ISceneTransitionHandler
    {
        public void OnBeforeTransition()
        {
            CameraManager.Instance.SetInput(false);
            // UI는 DontDestroyOnLoad라 이전 씬 대상을 붙든 페이지가 남음. HUD는 새 씬 PlayerController가 다시 연다.
            UIManager.Instance.ClearAllPages();
        }

        public void OnSceneLoaded(string sceneName, string entranceId)
        {
            if(string.IsNullOrEmpty(entranceId))
                return;

            SceneEntrance[] entrances = UnityEngine.Object.FindObjectsByType<SceneEntrance>(FindObjectsSortMode.None);
            foreach(SceneEntrance entrance in entrances)
            {
                if(entrance.EntranceId != entranceId)
                    continue;

                PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                if(player != null)
                    player.WarpToEntrance(entrance.SpawnPosition, entrance.StartNode);
                break;
            }
        }

        public void OnAfterTransition()
        {
            CameraManager.Instance.SetInput(true);
        }
    }

    /// <summary>
    /// 페이드 기반 씬 전환 매니저.
    /// ISceneTransitionHandler를 통해 프로젝트별 전환 로직을 주입합니다.
    /// </summary>
    public class SceneTransitionManager : Singleton<SceneTransitionManager>
    {
        #region Settings

        [Header("Transition Settings")]
        [SerializeField] private float fadeDuration = 0.4f;
        [SerializeField] private Color fadeColor = Color.black;

        #endregion

        #region State

        public event Action OnTransitionStarted;
        public event Action OnTransitionCompleted;

        public bool IsTransitioning { get; private set; }

        /// <summary>마지막으로 진입한 SceneEntrance ID. 세이브 데이터의 entranceId로 사용됩니다.</summary>
        public string CurrentEntranceId { get; private set; } = "";

        private Canvas _fadeCanvas;
        private Image _fadeImage;
        private ISceneTransitionHandler _handler;

        #endregion

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();
            CreateFadeCanvas();
            _handler = new DefaultSceneTransitionHandler();
        }

        #endregion

        #region Handler

        /// <summary>
        /// 씬 전환 핸들러를 교체합니다. 기본값은 DefaultSceneTransitionHandler입니다.
        /// </summary>
        public void SetHandler(ISceneTransitionHandler handler)
        {
            _handler = handler;
        }

        #endregion

        #region Public API

        /// <summary>
        /// 씬 전환 없이 화면만 페이드합니다 (시퀀스 연출용). to 1 = 완전히 가림. 가린 채로 씬 전환하면 깜박임 없이 이어집니다.
        /// </summary>
        public Awaitable FadeAsync(float to, float duration)
        {
            return FadeRoutine(CurrentFadeAlpha, to, duration);
        }

        public void TransitionTo(string sceneName, string entranceId = "")
        {
            if(IsTransitioning)
                return;
            ExecuteTransition(sceneName, entranceId).Forget();
        }

        #endregion

        #region Transition Flow

        private async Awaitable ExecuteTransition(string sceneName, string entranceId)
        {
            IsTransitioning = true;
            bool succeeded = false;

            try
            {
                OnTransitionStarted?.Invoke();

                _handler?.OnBeforeTransition();

                await FadeRoutine(CurrentFadeAlpha, 1f, fadeDuration);

                AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
                while(!op.isDone)
                    await Awaitable.NextFrameAsync();

                await Awaitable.NextFrameAsync();

                CurrentEntranceId = entranceId;
                _handler?.OnSceneLoaded(sceneName, entranceId);

                await FadeRoutine(1f, 0f, fadeDuration);

                succeeded = true;
            }
            finally
            {
                if(!succeeded && _fadeCanvas != null)
                {
                    SetFadeAlpha(0f);
                    _fadeCanvas.gameObject.SetActive(false);
                }

                IsTransitioning = false;
                _handler?.OnAfterTransition();
            }

            OnTransitionCompleted?.Invoke();
        }

        #endregion

        #region Fade

        private float CurrentFadeAlpha => _fadeImage != null && _fadeCanvas.gameObject.activeSelf ? _fadeImage.color.a : 0f;

        private async Awaitable FadeRoutine(float from, float to, float duration)
        {
            float timer = 0f;
            _fadeCanvas.gameObject.SetActive(true);
            SetFadeAlpha(from);

            while(timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, timer / duration);
                SetFadeAlpha(Mathf.Lerp(from, to, t));
                await Awaitable.NextFrameAsync();
            }

            SetFadeAlpha(to);

            if(to <= 0f)
                _fadeCanvas.gameObject.SetActive(false);
        }

        private void SetFadeAlpha(float alpha)
        {
            if(_fadeImage == null)
                return;
            Color c = _fadeImage.color;
            c.a = alpha;
            _fadeImage.color = c;
        }

        private void CreateFadeCanvas()
        {
            GameObject canvasGO = new GameObject("[TransitionFade]");
            canvasGO.transform.SetParent(transform);

            _fadeCanvas = canvasGO.AddComponent<Canvas>();
            _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _fadeCanvas.sortingOrder = 9999;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            GameObject imageGO = new GameObject("FadeImage");
            imageGO.transform.SetParent(canvasGO.transform, false);

            RectTransform rect = imageGO.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _fadeImage = imageGO.AddComponent<Image>();
            _fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            _fadeImage.raycastTarget = true;

            canvasGO.SetActive(false);
        }

        #endregion
    }
}
