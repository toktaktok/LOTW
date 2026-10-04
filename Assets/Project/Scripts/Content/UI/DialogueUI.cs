using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;
using Project.Scripts.System.UI;
using Project.Scripts.System.World;

namespace Project.Scripts.Content.UI
{
    /// <summary>
    /// 대화 UI. 단순 상호작용 프롬프트와 Dialogue 테이블 기반 다단계 대화를 모두 지원합니다.
    /// 대화 모드는 행의 nextId를 따라가며, choiceIds가 있으면 ConfirmButton을 복제해 선택지를 표시합니다.
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

        private DialogueData _currentLine;
        private bool _isDialogueMode;
        private readonly List<Button> _choiceButtons = new();

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

            // 선택지는 Submit/Navigate를 EventSystem이 처리하므로 Interact만 직접 받음
            if(_choiceButtons.Count > 0)
            {
                if(_controls.Player.Interact.WasPressedThisFrame())
                    SelectedChoice().onClick.Invoke();
                return;
            }

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
            _currentLine = null;
            _setupFrame = Time.frameCount;
            ClearChoices();

            SetSpeaker(null);

            if(promptText != null)
                promptText.text = interactable.InteractionPrompt;

            SetButtonVisibility(true);
        }

        /// <summary>
        /// 다단계 대화 모드. start 행부터 nextId를 따라 진행하며, 종료 행(nextId -1)을 지나면 자동으로 닫힙니다.
        /// </summary>
        public void SetupDialogue(DialogueData start, IInteractable source = null, GameObject interactor = null)
        {
            _pending = source;
            _interactor = interactor;
            _isDialogueMode = true;
            _setupFrame = Time.frameCount;

            ShowLine(start);
        }

        /// <summary>
        /// 대화 모드에서 다음 행으로 진행합니다. 선택지가 표시 중이면 무시합니다.
        /// </summary>
        public void AdvanceLine()
        {
            if(!_isDialogueMode || _choiceButtons.Count > 0)
                return;

            ShowLine(GetLine(_currentLine.nextId));
        }

        private void ShowLine(DialogueData line)
        {
            ClearChoices();

            if(line == null)
            {
                FinishDialogue();
                return;
            }

            _currentLine = line;
            SetSpeaker(line.speakerName);

            if(promptText != null)
                promptText.text = line.text;

            bool hasChoices = line.choiceIds != null && line.choiceIds.Length > 0;
            SetButtonVisibility(!hasChoices);
            if(hasChoices)
                ShowChoices(line.choiceIds);
        }

        private void ShowChoices(int[] choiceIds)
        {
            if(confirmButton == null)
                return;

            RectTransform template = (RectTransform)confirmButton.transform;
            float step = template.sizeDelta.y + UIDefines.ChoiceButtonSpacing;

            foreach(int choiceId in choiceIds)
            {
                DialogueData choice = GetLine(choiceId);
                if(choice == null)
                {
                    Debug.LogWarning($"[DialogueUI] 선택지 dataId {choiceId} 행이 없습니다.");
                    continue;
                }

                Button button = Instantiate(confirmButton, template.parent);
                button.gameObject.SetActive(true);
                ((RectTransform)button.transform).anchoredPosition =
                    template.anchoredPosition - new Vector2(0f, step * _choiceButtons.Count);

                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if(label != null)
                    label.text = choice.text;

                int nextId = choice.nextId;
                button.onClick.AddListener(() => OnChoose(nextId));
                _choiceButtons.Add(button);
            }

            // 유효한 선택지가 없으면 일반 진행으로 대체
            if(_choiceButtons.Count == 0)
            {
                SetButtonVisibility(true);
                return;
            }

            if(EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(_choiceButtons[0].gameObject);
        }

        private Button SelectedChoice()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            foreach(Button button in _choiceButtons)
            {
                if(button.gameObject == selected)
                    return button;
            }
            return _choiceButtons[0];
        }

        private void OnChoose(int nextId)
        {
            // Interact와 EventSystem Submit이 같은 프레임에 겹쳐 두 번 선택되는 것을 방지
            if(Time.frameCount == _setupFrame)
                return;

            _setupFrame = Time.frameCount;
            ShowLine(GetLine(nextId));
        }

        private void ClearChoices()
        {
            foreach(Button button in _choiceButtons)
            {
                if(button != null)
                    Destroy(button.gameObject);
            }
            _choiceButtons.Clear();
        }

        private static DialogueData GetLine(int dataId)
        {
            if(dataId < 0)
                return null;
            return DataManager.Instance.GetRow<DialogueData>(dataId);
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

        private void FinishDialogue()
        {
            ClearChoices();
            _isDialogueMode = false;
            _currentLine = null;
            _pending = null;
            UIManager.Instance.PopPage();
        }
    }
}
