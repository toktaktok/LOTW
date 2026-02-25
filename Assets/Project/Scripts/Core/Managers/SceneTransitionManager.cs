using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Project.Scripts.System.World;
using Project.Scripts.Content.Controller;

namespace Project.Scripts.Core.Managers
{
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

        private Canvas _fadeCanvas;
        private Image _fadeImage;

        #endregion

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();
            CreateFadeCanvas();
        }

        #endregion

        #region Public API

        /// <summary>
        /// 씬을 전환합니다. entranceId와 일치하는 SceneEntrance 위치에 플레이어를 배치합니다.
        /// </summary>
        public void TransitionTo(string sceneName, string entranceId = "")
        {
            if (IsTransitioning) return;
            ExecuteTransition(sceneName, entranceId).Cancel();
        }

        #endregion

        #region Transition Flow

        private async Awaitable ExecuteTransition(string sceneName, string entranceId)
        {
            IsTransitioning = true;
            OnTransitionStarted?.Invoke();

            CameraManager.Instance.SetInput(false);

            // 1. 화면을 검게 페이드
            await FadeRoutine(0f, 1f);

            // 2. 씬 비동기 로드
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone)
                await Awaitable.NextFrameAsync();

            // 3. 씬의 Start() 호출 완료 대기
            await Awaitable.NextFrameAsync();

            // 4. 플레이어를 입장 지점에 배치
            if (!string.IsNullOrEmpty(entranceId))
                PlacePlayerAtEntrance(entranceId);

            // 5. 화면 페이드 인
            await FadeRoutine(1f, 0f);

            CameraManager.Instance.SetInput(true);
            IsTransitioning = false;
            OnTransitionCompleted?.Invoke();
        }

        private void PlacePlayerAtEntrance(string entranceId)
        {
            SceneEntrance[] entrances = FindObjectsByType<SceneEntrance>(FindObjectsSortMode.None);
            foreach (SceneEntrance entrance in entrances)
            {
                if (entrance.EntranceId != entranceId) continue;

                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                    player.WarpToEntrance(entrance.SpawnPosition, entrance.StartNode);
                break;
            }
        }

        #endregion

        #region Fade

        private async Awaitable FadeRoutine(float from, float to)
        {
            float timer = 0f;
            _fadeCanvas.gameObject.SetActive(true);
            SetFadeAlpha(from);

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, timer / fadeDuration);
                SetFadeAlpha(Mathf.Lerp(from, to, t));
                await Awaitable.NextFrameAsync();
            }

            SetFadeAlpha(to);

            if (to <= 0f)
                _fadeCanvas.gameObject.SetActive(false);
        }

        private void SetFadeAlpha(float alpha)
        {
            if (_fadeImage == null) return;
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
