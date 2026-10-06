using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Project.Scripts.Content.Dialogue;
using Project.Scripts.Core;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;
using Project.Scripts.System.Dialogue;
using Project.Scripts.System.World;
using Project.Scripts.Framework;
using Project.Scripts.Framework.Managers;
using Project.Scripts.Framework.UI;

namespace Project.Scripts.Content.UI
{
    /// <summary>
    /// 대화 UI. 단순 상호작용 프롬프트와 Dialogue 테이블 기반 다단계 대화를 모두 지원합니다.
    /// 대화 모드는 행의 nextId를 따라가며, choiceIds가 있으면 ChoicePanel의 버튼에 선택지를 표시합니다.
    /// 행의 conditions/actions와 분기 행은 DialogueCommands가 처리합니다.
    /// </summary>
    public class DialogueUI : BaseUI
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("Choices")]
        [SerializeField] private GameObject choicePanel;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private TMP_Text[] choiceLabels;

        private IInteractable _pending;
        private GameObject _interactor;

        private DialogueData _currentLine;
        private bool _isDialogueMode;
        private Action _onFinished;
        // 현재 표시 중인 선택지 버튼 (choiceButtons 앞쪽부터 사용)
        private readonly List<Button> _choiceButtons = new();
        private readonly IDialogueContext _context = new ManagerDialogueContext();

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
            _onFinished = null;
            _setupFrame = Time.frameCount;
            ClearChoices();

            SetSpeaker(null);

            if(promptText != null)
                promptText.text = Localization.Resolve(interactable.InteractionPrompt);

            SetButtonVisibility(true);
        }

        /// <summary>
        /// 다단계 대화 모드. start 행부터 nextId를 따라 진행하며, 종료 행(nextId -1)을 지나면 자동으로 닫힙니다.
        /// onFinished는 대화가 닫힌 뒤 호출됩니다.
        /// </summary>
        public void SetupDialogue(DialogueData start, IInteractable source = null, GameObject interactor = null, Action onFinished = null)
        {
            _pending = source;
            _interactor = interactor;
            _isDialogueMode = true;
            _onFinished = onFinished;
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

            line = DialogueCommands.ResolveRoute(line, GetLine, _context);
            if(line == null)
            {
                FinishDialogue();
                return;
            }

            _currentLine = line;
            DialogueCommands.RunActions(line.actions, _context);
            // minigame:id 행은 표시하지 않고 미니게임을 연 뒤 다음 행에서 이어감
            if(TryStartMinigame(line.actions, line.nextId))
                return;

            SetSpeaker(Localization.Resolve(line.speakerName));

            if(promptText != null)
                promptText.text = Localization.Resolve(line.text);

            bool hasChoices = line.choiceIds != null && line.choiceIds.Length > 0;
            SetButtonVisibility(!hasChoices);
            if(hasChoices)
                ShowChoices(line.choiceIds);
        }

        private void ShowChoices(int[] choiceIds)
        {
            if(choiceButtons == null)
                return;

            foreach(int choiceId in choiceIds)
            {
                DialogueData choice = GetLine(choiceId);
                if(choice == null)
                {
                    Debug.LogWarning($"[DialogueUI] 선택지 dataId {choiceId} 행이 없습니다.");
                    continue;
                }
                if(!DialogueCommands.CheckConditions(choice.conditions, _context))
                    continue;

                int index = _choiceButtons.Count;
                if(index >= choiceButtons.Length)
                {
                    Debug.LogWarning($"[DialogueUI] 선택지 버튼({choiceButtons.Length}개)보다 선택지가 많아 dataId {choiceId}부터 생략합니다.");
                    break;
                }

                Button button = choiceButtons[index];
                button.gameObject.SetActive(true);
                if(choiceLabels != null && index < choiceLabels.Length)
                    choiceLabels[index].text = Localization.Resolve(choice.text);

                button.onClick.AddListener(() => OnChoose(choice));
                _choiceButtons.Add(button);
            }

            // 유효한 선택지가 없으면 일반 진행으로 대체
            if(_choiceButtons.Count == 0)
            {
                SetButtonVisibility(true);
                return;
            }

            if(choicePanel != null)
                choicePanel.SetActive(true);

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

        private void OnChoose(DialogueData choice)
        {
            // Interact와 EventSystem Submit이 같은 프레임에 겹쳐 두 번 선택되는 것을 방지
            if(Time.frameCount == _setupFrame)
                return;

            _setupFrame = Time.frameCount;
            DialogueCommands.RunActions(choice.actions, _context);
            if(TryStartMinigame(choice.actions, choice.nextId))
                return;

            ShowLine(GetLine(choice.nextId));
        }

        /// <summary>
        /// actions에 minigame:id 가 있고 시작할 수 있으면 대화를 닫고 미니게임을 엽니다 (둘은 동시에 떠 있지 않음).
        /// 끝나면 결과의 followDialogueId(없으면 continueId) 행에서 대화를 다시 엽니다.
        /// 시작할 수 없으면 false를 돌려주어 대화가 그대로 이어집니다.
        /// </summary>
        private bool TryStartMinigame(string actions, int continueId)
        {
            if(!DialogueCommands.TryGetMinigameId(actions, out string minigameId))
                return false;
            if(!MinigameManager.Instance.TryGetDefinition(minigameId, out MinigameDefinition definition) ||
               !MinigameManager.Instance.CanStart(definition))
                return false;

            IInteractable source = _pending;
            GameObject interactor = _interactor;
            Action onFinished = _onFinished;

            // onFinished(카메라 복귀 등)는 미니게임 뒤 대화가 실제로 끝날 때까지 미룸
            _onFinished = null;
            CloseDialogue();

            GameObject sourceObject = source is Component component ? component.gameObject : null;
            MinigameManager.Instance.Play(definition, sourceObject, result =>
            {
                DialogueData next = GetLine(result.followDialogueId >= 0 ? result.followDialogueId : continueId);
                if(next == null)
                    onFinished?.Invoke();
                else
                    UIManager.Instance.PushPage<DialogueUI>(UILayer.Popup, ui => ui.SetupDialogue(next, source, interactor, onFinished));
            });
            return true;
        }

        private void ClearChoices()
        {
            _choiceButtons.Clear();
            if(choiceButtons != null)
            {
                foreach(Button button in choiceButtons)
                {
                    button.onClick.RemoveAllListeners();
                    button.gameObject.SetActive(false);
                }
            }
            if(choicePanel != null)
                choicePanel.SetActive(false);
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
            Action onFinished = _onFinished;
            _onFinished = null;
            CloseDialogue();
            onFinished?.Invoke();
        }

        private void CloseDialogue()
        {
            ClearChoices();
            _isDialogueMode = false;
            _currentLine = null;
            _pending = null;
            UIManager.Instance.PopPage();
        }
    }
}
