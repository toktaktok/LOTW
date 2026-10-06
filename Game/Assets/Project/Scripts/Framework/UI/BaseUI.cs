using UnityEngine;

namespace Project.Scripts.Framework.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseUI : MonoBehaviour
    {
        #region Properties

        [Header("Base UI Status")]
        [SerializeField]
        protected UIState currentUIState = UIState.None;
        
        [Header("Settings")]
        [SerializeField]
        protected UITransitionMode transitionMode = UITransitionMode.Fade;
        [SerializeField]
        private bool hideOnAwake = true;
        [SerializeField]
        private float fadeDuration = 0.3f;
        
        [Header("Animation")]
        [SerializeField]
        protected Animator uiAnimator;
        
        public UIState CurrentUIState => currentUIState;
        public bool IsVisible => currentUIState == UIState.Opening || currentUIState == UIState.Open;
        
        protected CanvasGroup canvasGroup;

        private static readonly int ShowHash = Animator.StringToHash("Show");
        private static readonly int HideHash = Animator.StringToHash("Hide");
        
        #endregion

        #region Methods

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if(uiAnimator == null)
                uiAnimator = GetComponent<Animator>();
            
            if(hideOnAwake)
            {
                SetVisibility(false);
                currentUIState = UIState.None;
            }
        }

        // --- Show ---
        public virtual async Awaitable ShowAsync()
        {
            if(currentUIState == UIState.Opening || currentUIState == UIState.Open)
                return;
            
            currentUIState = UIState.Opening;
            gameObject.SetActive(true);

            switch(transitionMode)
            {
                case UITransitionMode.Fade:
                    await FadeRoutine(0f, 1f);
                    break;
                case UITransitionMode.Animation:
                    await PlayAnimationAndWait(ShowHash);
                    break;
            }
            SetVisibility(true);
        }
        
        // --- Hide ---
        public virtual async Awaitable HideAsync()
        {
            if(currentUIState == UIState.Closing || currentUIState == UIState.Closed)
                return;

            currentUIState = UIState.Closing;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            switch(transitionMode)
            {
                case UITransitionMode.Fade:
                    await FadeRoutine(1f, 0f);
                    break;
                case UITransitionMode.Animation:
                    await PlayAnimationAndWait(HideHash);
                    break;
            }
            SetVisibility(false);
        }
        
        // --- Async ---
        protected async Awaitable FadeRoutine(float start, float end)
        {
            float timer = 0f;
            canvasGroup.alpha = start;

            while(timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, timer / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(start, end, t);
            
                await Awaitable.NextFrameAsync();
            }
            canvasGroup.alpha = end;
        }
        protected async Awaitable PlayAnimationAndWait(int triggerID)
        {
            if(uiAnimator == null)
                return;

            uiAnimator.SetTrigger(triggerID);
            await Awaitable.NextFrameAsync();

            float animLength = uiAnimator.GetCurrentAnimatorStateInfo(0).length;
            // timeScale 0(일시정지)에서도 진행되도록 unscaled 시간으로 기다린다.
            float timer = 0f;
            while(timer < animLength)
            {
                timer += Time.unscaledDeltaTime;
                await Awaitable.NextFrameAsync();
            }
        }
        
        /// <summary>
        /// 취소 입력(Esc, 게임패드 B/Start)이 최상단 페이지인 이 UI 에 왔을 때 호출됩니다 (MenuInput).
        /// 스스로 닫았으면 true. 기본은 무시(대화처럼 취소로 닫히면 안 되는 UI).
        /// </summary>
        public virtual bool OnCancel() => false;

        /// <summary>위에 다른 페이지가 열렸을 때 키보드/패드 이동이 이 UI 로 새지 않도록 상호작용만 끄고 켠다.</summary>
        public void SetInteractable(bool interactable)
        {
            if(canvasGroup != null)
                canvasGroup.interactable = interactable;
        }

        public void SetVisibility(bool visible)
        {
            if(canvasGroup == null)
                return;
            
            SetCanvasState((visible? 1f:0f), visible);
            gameObject.SetActive(visible);
            currentUIState = visible ? UIState.Open : UIState.Closed;
        }
        private void SetCanvasState(float alpha, bool interactable)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = canvasGroup.interactable = interactable;
        }

        #endregion
    }
}
