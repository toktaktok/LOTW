using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.System.UI;
using Project.Scripts.System.World;

namespace Project.Scripts.Content.UI
{
    /// <summary>
    /// 대화 UI. 단순 상호작용 프롬프트와 다단계 대화를 모두 지원합니다.
    /// </summary>
    public class DialogueUI : BaseUI
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private IInteractable _pending;
        private GameObject _interactor;

        private DialogueLine[] _lines;
        private int _lineIndex;
        private bool _isDialogueMode;

        private PlayerControls _controls;
        private int _setupFrame = -1;

        protected override void Awake()
        {
            // base.Awake()에서 비활성화되며 OnDisable이 호출되므로 먼저 생성
            _controls = new PlayerControls();
            base.Awake();
            confirmButton?.onClick.AddListener(OnConfirm);
            cancelButton?.onClick.AddListener(OnCancel);
        }

        private void OnEnable() => _controls.Enable();
        private void OnDisable() => _controls.Disable();

        private void OnDestroy() => _controls.Dispose();

        private void Update()
        {
            if(!IsVisible || Time.frameCount == _setupFrame)
                return;

            if(!_controls.Player.Interact.WasPressedThisFrame() && !_controls.UI.Submit.WasPressedThisFrame())
                return;

            if(_isDialogueMode)
                AdvanceLine();
            else
                OnConfirm();
        }

        /// <summary>
        /// 기존 단순 상호작용 프롬프트 모드.
        /// </summary>
        public void Setup(IInteractable interactable, GameObject interactor)
        {
            _pending = interactable;
            _interactor = interactor;
            _isDialogueMode = false;
            _lines = null;
            _lineIndex = 0;
            _setupFrame = Time.frameCount;

            SetSpeaker(null);

            if(promptText != null)
                promptText.text = interactable.InteractionPrompt;

            SetButtonVisibility(true);
        }

        /// <summary>
        /// 다단계 대화 모드. 대화가 끝나면 자동으로 닫힙니다.
        /// </summary>
        public void SetupDialogue(DialogueData data, IInteractable source = null, GameObject interactor = null)
        {
            _pending = source;
            _interactor = interactor;
            _isDialogueMode = true;
            _lines = data.lines;
            _lineIndex = 0;
            _setupFrame = Time.frameCount;

            SetButtonVisibility(true);
            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            if(_lines == null || _lineIndex >= _lines.Length)
            {
                FinishDialogue();
                return;
            }

            DialogueLine line = _lines[_lineIndex];
            SetSpeaker(line.speaker);

            if(promptText != null)
                promptText.text = line.text;
        }

        private void SetSpeaker(string speaker)
        {
            if(speakerText == null)
                return;

            if(string.IsNullOrEmpty(speaker))
            {
                speakerText.gameObject.SetActive(false);
            }
            else
            {
                speakerText.gameObject.SetActive(true);
                speakerText.text = speaker;
            }
        }

        private void SetButtonVisibility(bool visible)
        {
            if(confirmButton != null)
                confirmButton.gameObject.SetActive(visible);
            if(cancelButton != null)
                cancelButton.gameObject.SetActive(visible);
        }

        private void OnConfirm()
        {
            if(_isDialogueMode)
            {
                AdvanceLine();
                return;
            }

            // Interact가 같은 DialogueUI를 다시 열 수 있으므로 닫기를 먼저 큐에 넣음
            IInteractable pending = _pending;
            GameObject interactor = _interactor;
            _pending = null;
            UIManager.Instance.PopPage();
            pending?.Interact(interactor);
        }

        private void OnCancel()
        {
            if(_isDialogueMode)
            {
                FinishDialogue();
                return;
            }

            _pending = null;
            UIManager.Instance.PopPage();
        }

        /// <summary>
        /// 대화 모드에서 다음 줄로 진행합니다. 외부(입력 시스템)에서 호출 가능.
        /// </summary>
        public void AdvanceLine()
        {
            if(!_isDialogueMode)
                return;

            _lineIndex++;
            ShowCurrentLine();
        }

        private void FinishDialogue()
        {
            _isDialogueMode = false;
            _lines = null;
            _lineIndex = 0;
            _pending = null;
            UIManager.Instance.PopPage();
        }
    }
}
