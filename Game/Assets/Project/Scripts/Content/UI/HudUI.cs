using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project.Scripts.Content.Story;
using Project.Scripts.Core;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;
using Project.Scripts.Framework;
using Project.Scripts.Framework.Managers;
using Project.Scripts.Framework.UI;

namespace Project.Scripts.Content.UI
{
    /// <summary>
    /// 메인 게임플레이 HUD.
    /// 상호작용 힌트, 수첩 버튼(안 읽은 항목 표시), 진행 중 의뢰 목록, "수첩에 기록됨" 알림을 표시합니다.
    /// 수첩/의뢰 칸은 프리팹에 없으면 건너뜁니다.
    /// </summary>
    public class HudUI : BaseUI
    {
        [Header("Interaction Hint")]
        [SerializeField] private GameObject interactionHintRoot;
        [SerializeField] private TMP_Text interactionHintText;

        [Header("Notebook")]
        [SerializeField] private Button notebookButton;
        [SerializeField] private GameObject unreadBadge;
        [SerializeField] private TMP_Text unreadCountText;
        [Tooltip("수첩 버튼 모서리의 키 표시 (Notebook 액션 바인딩)")]
        [SerializeField] private TMP_Text notebookKeyText;

        [Header("Quest List")]
        [Tooltip("진행 중 의뢰가 없으면 숨기는 목록 배경")]
        [SerializeField] private GameObject questListRoot;
        [Tooltip("위에서부터 UIDefines.HudQuestLines 줄. 메인 의뢰가 첫 줄")]
        [SerializeField] private TMP_Text[] questLines;

        [Header("Toast")]
        [SerializeField] private GameObject toastRoot;
        [SerializeField] private TMP_Text toastText;

        private readonly Queue<string> _toasts = new Queue<string>();
        private Coroutine _toastRoutine;
        private bool _hintVisible;
        private bool _dirty;

        protected override void Awake()
        {
            base.Awake();
            if(notebookButton != null)
                notebookButton.onClick.AddListener(OnNotebook);
        }

        private void OnEnable()
        {
            FlagManager.OnFlagChanged += OnFlagChanged;
            SaveManager.OnGameSaved += OnGameSaved;
            if(toastRoot != null)
                toastRoot.SetActive(false);
            if(notebookKeyText != null)
                notebookKeyText.text = MenuInput.Instance.NotebookKeyLabel;
            RefreshStory();
        }

        // 씬 전환으로 HUD가 닫혔다 다시 열릴 때 이전 씬의 힌트/알림이 남지 않도록 함
        private void OnDisable()
        {
            FlagManager.OnFlagChanged -= OnFlagChanged;
            SaveManager.OnGameSaved -= OnGameSaved;
            HideInteractionHint();
            _toasts.Clear();
            _toastRoutine = null;
        }

        // 목표 조건은 아무 플래그에나 걸릴 수 있으므로 변경을 모아 프레임당 한 번 갱신
        private void LateUpdate()
        {
            if(!_dirty)
                return;

            _dirty = false;
            RefreshStory();
        }

        public void ShowInteractionHint(string prompt)
        {
            if(interactionHintRoot == null)
                return;

            interactionHintText.text = Localization.Resolve(prompt);

            if(!_hintVisible)
            {
                interactionHintRoot.SetActive(true);
                _hintVisible = true;
            }
        }

        public void HideInteractionHint()
        {
            if(!_hintVisible || interactionHintRoot == null)
                return;

            interactionHintRoot.SetActive(false);
            _hintVisible = false;
        }

        public void ShowToast(string text)
        {
            if(toastRoot == null || toastText == null)
                return;

            _toasts.Enqueue(Localization.Resolve(text));
            if(_toastRoutine == null && isActiveAndEnabled)
                _toastRoutine = StartCoroutine(ToastRoutine());
        }

        private void OnGameSaved(int slot)
        {
            ShowToast("@ui.toast.saved");
        }

        private void OnFlagChanged(string key)
        {
            _dirty = true;

            if(StoryKeys.TryGetId(key, StoryKeys.NotePrefix, out int noteId))
            {
                if(NotebookLog.GetState(noteId) == NoteState.Unread)
                    ShowToast("@ui.toast.note");
            }
            else if(StoryKeys.TryGetId(key, StoryKeys.QuestPrefix, out int questId))
            {
                QuestState state = QuestLog.GetState(questId);
                QuestData quest = DataManager.Instance.GetRow<QuestData>(questId);
                string title = quest != null ? Localization.Resolve(quest.title) : string.Empty;
                if(state == QuestState.Active)
                    ShowToast(string.Format(Localization.Resolve("@ui.toast.quest"), title));
                else if(state == QuestState.Done)
                    ShowToast(string.Format(Localization.Resolve("@ui.toast.questdone"), title));
            }
        }

        private void RefreshStory()
        {
            RefreshUnreadBadge();
            RefreshQuestLines();
        }

        private void RefreshUnreadBadge()
        {
            if(unreadBadge == null)
                return;

            int count = NotebookLog.GetUnreadCount();
            unreadBadge.SetActive(count > 0);
            if(unreadCountText != null)
                unreadCountText.text = count.ToString();
        }

        private void RefreshQuestLines()
        {
            if(questLines == null || questLines.Length == 0)
                return;

            List<QuestData> active = QuestLog.GetActive();
            if(questListRoot != null)
                questListRoot.SetActive(active.Count > 0);
            int lineCount = Mathf.Min(questLines.Length, UIDefines.HudQuestLines);
            for(int i = 0; i < questLines.Length; i++)
            {
                TMP_Text line = questLines[i];
                if(line == null)
                    continue;

                bool show = i < lineCount && i < active.Count;
                line.gameObject.SetActive(show);
                if(show)
                {
                    string title = Localization.Resolve(active[i].title);
                    line.text = active[i].IsMain ? $"<b>{title}</b>" : title;
                }
            }
        }

        private IEnumerator ToastRoutine()
        {
            while(_toasts.Count > 0)
            {
                toastText.text = _toasts.Dequeue();
                toastRoot.SetActive(true);
                yield return new WaitForSecondsRealtime(UIDefines.ToastDuration);
                toastRoot.SetActive(false);
            }
            _toastRoutine = null;
        }

        private void OnNotebook()
        {
            if(UIManager.Instance.HasBlockingPage || SequencePlayer.IsPlaying)
                return;

            UIManager.Instance.PushPage<NotebookUI>(UILayer.Popup);
        }
    }
}
