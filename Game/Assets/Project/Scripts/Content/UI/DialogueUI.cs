using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.System.UI;
using Project.Scripts.System.World;
using DialogueRow = Project.Scripts.Data.Table.DialogueData;

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

        [Header("Choices")]
        [SerializeField] private GameObject choicePanel;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private TMP_Text[] choiceLabels;

        private IInteractable _pending;
        private GameObject _interactor;

        private DialogueLine[] _lines;
        private int _lineIndex;
        private bool _isDialogueMode;
        private DialogueRow _tableRow;
        private Action _onFinished;

        private PlayerControls _controls;
        private int _setupFrame = -1;

        protected override void Awake()
        {
            // base.Awake()에서 비활성화되며 OnDisable이 호출되므로 먼저 생성
            _controls = new PlayerControls();
            base.Awake();
            confirmButton?.onClick.AddListener(OnConfirm);
            cancelButton?.onClick.AddListener(OnCancel);

            if(choiceButtons != null)
            {
                for(int i = 0; i < choiceButtons.Length; i++)
                {
                    int index = i;
                    choiceButtons[i].onClick.AddListener(() => OnChoice(index));
                }
            }
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
            _tableRow = null;
            _onFinished = null;
            _setupFrame = Time.frameCount;

            SetSpeaker(null);

            if(promptText != null)
                promptText.text = interactable.InteractionPrompt;

            SetButtonVisibility(true);
            SetChoices(null);
        }

        /// <summary>
        /// 다단계 대화 모드. 대화가 끝나면 자동으로 닫힙니다.
        /// </summary>
        public void SetupDialogue(DialogueData data, IInteractable source = null, GameObject interactor = null, Action onFinished = null)
        {
            _pending = source;
            _interactor = interactor;
            _isDialogueMode = true;
            _lines = data.lines;
            _lineIndex = 0;
            _tableRow = null;
            _onFinished = onFinished;
            _setupFrame = Time.frameCount;

            SetButtonVisibility(true);
            SetChoices(null);
            ShowCurrentLine();
        }

        /// <summary>
        /// 대화 테이블(Resources/Table/Dialogue.json) 모드. startId 행부터 nextId/choiceIds를 따라 진행합니다.
        /// nextId -1 또는 없는 행이면 닫힙니다.
        /// </summary>
        public void SetupTableDialogue(int startId, IInteractable source, GameObject interactor, Action onFinished = null)
        {
            _pending = source;
            _interactor = interactor;
            _isDialogueMode = true;
            _lines = null;
            _lineIndex = 0;
            _onFinished = onFinished;
            _setupFrame = Time.frameCount;

            ShowTableRow(startId);
        }

        private void ShowTableRow(int dataId)
        {
            _tableRow = dataId == -1 ? null : GetTableRow(dataId);
            if(_tableRow == null)
            {
                FinishDialogue();
                return;
            }

            SetSpeaker(_tableRow.speakerName);

            if(promptText != null)
                promptText.text = _tableRow.text;

            // 선택지 중에는 확인/취소 버튼을 숨겨 메뉴를 건너뛰지 못하게 함
            bool hasChoices = HasChoices;
            SetButtonVisibility(!hasChoices);
            SetChoices(hasChoices ? _tableRow.choiceIds : null);
        }

        private bool HasChoices => _tableRow != null && _tableRow.choiceIds != null && _tableRow.choiceIds.Length > 0;

        private DialogueRow GetTableRow(int dataId)
        {
            DialogueRow row = DataManager.Instance.GetRow<DialogueRow>(dataId);
            if(row == null)
                Debug.LogWarning($"[DialogueUI] 대화 행을 찾을 수 없습니다: {dataId}");
            return row;
        }

        private void SetChoices(int[] choiceIds)
        {
            bool visible = choiceIds != null;
            if(choicePanel != null)
                choicePanel.SetActive(visible);

            if(choiceButtons == null)
                return;

            for(int i = 0; i < choiceButtons.Length; i++)
            {
                bool active = visible && i < choiceIds.Length;
                choiceButtons[i].gameObject.SetActive(active);
                if(!active || choiceLabels == null || i >= choiceLabels.Length)
                    continue;

                DialogueRow choice = GetTableRow(choiceIds[i]);
                choiceLabels[i].text = choice != null ? choice.text : string.Empty;
            }

            // 키보드/게임패드로도 고를 수 있게 첫 선택지를 선택. 이 프레임의 Submit은 OnChoice에서 무시
            if(visible && choiceIds.Length > 0 && choiceButtons.Length > 0 && EventSystem.current != null)
            {
                _setupFrame = Time.frameCount;
                EventSystem.current.SetSelectedGameObject(choiceButtons[0].gameObject);
            }
        }

        private void OnChoice(int index)
        {
            if(!HasChoices || index >= _tableRow.choiceIds.Length || Time.frameCount == _setupFrame)
                return;

            // 같은 프레임의 Submit 입력이 다음 줄까지 넘기지 않도록 함
            _setupFrame = Time.frameCount;
            DialogueRow choice = GetTableRow(_tableRow.choiceIds[index]);
            ShowTableRow(choice != null ? choice.nextId : -1);
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

            if(_tableRow != null)
            {
                // 선택지 중에는 Interact/Submit 무시
                if(!HasChoices)
                    ShowTableRow(_tableRow.nextId);
                return;
            }

            _lineIndex++;
            ShowCurrentLine();
        }

        private void FinishDialogue()
        {
            _isDialogueMode = false;
            _lines = null;
            _lineIndex = 0;
            _tableRow = null;
            _pending = null;
            SetChoices(null);
            UIManager.Instance.PopPage();

            Action onFinished = _onFinished;
            _onFinished = null;
            onFinished?.Invoke();
        }
    }
}
