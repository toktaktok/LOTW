using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

using Project.Scripts.Data;

namespace Project.Scripts.System.UI
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

            switch (transitionMode)
            {
                case UITransitionMode.Fade:
                    await FadeRoutine(0f, 1f);
                    break;
                case UITransitionMode.Animation:
                    await PlayAnimationAndWait(UIDefines.AnimShow);
                    break;
            }
            SetVisibility(true);
        }
        
        // --- Hide ---
        public virtual async Awaitable HideAsync()
        {
            if(currentUIState == UIState.Closing || currentUIState == UIState.Closed) return;

            currentUIState = UIState.Closing;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            switch (transitionMode)
            {
                case UITransitionMode.Fade:
                    await FadeRoutine(1f, 0f);
                    break;
                case UITransitionMode.Animation:
                    await PlayAnimationAndWait(UIDefines.AnimHide);
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
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, timer / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(start, end, t);
            
                await Awaitable.NextFrameAsync();
            }
            canvasGroup.alpha = end;
        }
        protected async Awaitable PlayAnimationAndWait(int triggerID)
        {
            if (uiAnimator == null) return;

            uiAnimator.SetTrigger(triggerID);
            await Awaitable.NextFrameAsync();

            float animLength = uiAnimator.GetCurrentAnimatorStateInfo(0).length;
            await Awaitable.WaitForSecondsAsync(animLength);
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
