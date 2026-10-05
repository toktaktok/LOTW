using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Project.Scripts.Core;
using Project.Scripts.Core.Managers;
using Project.Scripts.System.UI;

namespace Project.Scripts.Content.UI
{
    /// <summary>
    /// 예/아니오 확인 팝업. UIManager.PushPage&lt;ConfirmUI&gt;(UILayer.System, ui =&gt; ui.Setup(...)) 로 엽니다.
    /// 어느 버튼이든 먼저 닫고 콜백을 부릅니다.
    /// </summary>
    public class ConfirmUI : BaseUI
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        private Action _onYes;
        private Action _onNo;

        protected override void Awake()
        {
            base.Awake();
            if(yesButton != null)
                yesButton.onClick.AddListener(() => Answer(true));
            if(noButton != null)
                noButton.onClick.AddListener(() => Answer(false));
        }

        public void Setup(string message, Action onYes, Action onNo = null)
        {
            _onYes = onYes;
            _onNo = onNo;
            if(messageText != null)
                messageText.text = Localization.Resolve(message);

            // 실수로 덮어쓰지 않도록 기본 선택은 '아니오'
            if(noButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(noButton.gameObject);
        }

        private void Answer(bool yes)
        {
            Action callback = yes ? _onYes : _onNo;
            _onYes = null;
            _onNo = null;
            UIManager.Instance.Close<ConfirmUI>();
            callback?.Invoke();
        }
    }
}
