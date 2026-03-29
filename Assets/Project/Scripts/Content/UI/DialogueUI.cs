using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Project.Scripts.Core.Managers;
using Project.Scripts.System.UI;
using Project.Scripts.System.World;

namespace Project.Scripts.Content.UI
{
    public class DialogueUI : BaseUI
    {
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private IInteractable _pending;
        private GameObject _interactor;

        protected override void Awake()
        {
            base.Awake();
            confirmButton?.onClick.AddListener(OnConfirm);
            cancelButton?.onClick.AddListener(OnCancel);
        }

        public void Setup(IInteractable interactable, GameObject interactor)
        {
            _pending = interactable;
            _interactor = interactor;

            if(promptText != null)
                promptText.text = interactable.InteractionPrompt;
        }

        private void OnConfirm()
        {
            _pending?.Interact(_interactor);
            _pending = null;
            UIManager.Instance.PopPage();
        }

        private void OnCancel()
        {
            _pending = null;
            UIManager.Instance.PopPage();
        }
    }
}
