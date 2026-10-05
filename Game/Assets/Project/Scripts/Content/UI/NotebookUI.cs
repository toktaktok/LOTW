using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project.Scripts.Content.Story;
using Project.Scripts.Core;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;
using Project.Scripts.System.UI;

namespace Project.Scripts.Content.UI
{
    /// <summary>
    /// 탐정 수첩. 표지(진행 중 의뢰 포스트잇)와 탭별 목록(왼쪽) / 상세(오른쪽)를 보여줍니다.
    /// 내용은 플래그와 테이블에서 매번 다시 만들고, 플래그가 바뀌면 다음 LateUpdate 에 갱신합니다.
    /// </summary>
    public class NotebookUI : BaseUI
    {
        private const string UnreadColor = "#A3402C";

        [Header("Tabs")]
        [Tooltip("NotebookTab 순서대로 할당")]
        [SerializeField] private Button[] tabButtons;

        [Header("Cover")]
        [SerializeField] private GameObject coverRoot;
        [SerializeField] private Transform stickyContainer;
        [SerializeField] private Image stickyTemplate;
        [SerializeField] private Sprite mainStickySprite;
        [SerializeField] private Sprite subStickySprite;

        [Header("List Page")]
        [SerializeField] private GameObject listRoot;
        [SerializeField] private TMP_Text pageTitle;
        [SerializeField] private Transform entryContainer;
        [SerializeField] private Button entryTemplate;
        [SerializeField] private TMP_Text emptyText;

        [Header("Detail Page")]
        [SerializeField] private TMP_Text detailTitle;
        [SerializeField] private TMP_Text detailBody;

        [SerializeField] private Button closeButton;

        private readonly List<Button> _entries = new List<Button>();
        private readonly List<Image> _stickies = new List<Image>();
        private List<NotebookEntryView> _views = new List<NotebookEntryView>();
        private NotebookTab _tab = NotebookTab.Cover;
        private int _selectedId;
        private bool _dirty;

        protected override void Awake()
        {
            base.Awake();
            if(closeButton != null)
                closeButton.onClick.AddListener(OnClose);

            if(tabButtons != null)
            {
                for(int i = 0; i < tabButtons.Length; i++)
                {
                    NotebookTab tab = (NotebookTab)i;
                    if(tabButtons[i] != null)
                        tabButtons[i].onClick.AddListener(() => SetupTab(tab));
                }
            }

            if(entryTemplate != null)
                entryTemplate.gameObject.SetActive(false);
            if(stickyTemplate != null)
                stickyTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            FlagManager.OnFlagChanged += OnFlagChanged;
            Refresh();
        }

        private void OnDisable()
        {
            FlagManager.OnFlagChanged -= OnFlagChanged;
        }

        // 항목 클릭 중(읽음 처리)에 버튼을 다시 만들지 않도록 한 프레임 미룸
        private void LateUpdate()
        {
            if(!_dirty)
                return;

            _dirty = false;
            Refresh();
        }

        public void SetupTab(NotebookTab tab)
        {
            if(_tab != tab)
            {
                _tab = tab;
                _selectedId = 0;
            }
            Refresh();
        }

        private void OnFlagChanged(string key)
        {
            if(StoryKeys.IsQuestKey(key) || StoryKeys.IsNoteKey(key) || key.StartsWith(StoryKeys.MetPrefix))
                _dirty = true;
        }

        private void Refresh()
        {
            UpdateTabButtons();

            bool isCover = _tab == NotebookTab.Cover;
            if(coverRoot != null)
                coverRoot.SetActive(isCover);
            if(listRoot != null)
                listRoot.SetActive(!isCover);

            if(isCover)
            {
                RefreshCover();
                return;
            }

            _views = BuildViews(_tab);
            if(pageTitle != null)
                pageTitle.text = Localization.Resolve(TabTitleKey(_tab));
            RefreshEntries();

            if(_selectedId == 0 || !ContainsSelectable(_selectedId))
                _selectedId = FirstSelectableId();
            ShowDetail(_selectedId);
        }

        private void UpdateTabButtons()
        {
            if(tabButtons == null)
                return;

            for(int i = 0; i < tabButtons.Length; i++)
            {
                if(tabButtons[i] != null)
                    tabButtons[i].interactable = i != (int)_tab;
            }
        }

        private void RefreshCover()
        {
            if(stickyTemplate == null || stickyContainer == null)
                return;

            var notes = NotebookPageBuilder.BuildStickyNotes(QuestLog.GetActive(), q => QuestLog.GetObjectives(q),
                QuestLog.IsObjectiveDone, UIDefines.MaxStickyNotes);

            for(int i = 0; i < notes.Count; i++)
            {
                Image sticky = GetOrCreate(_stickies, stickyTemplate, stickyContainer, i);
                Sprite sprite = notes[i].isMain ? mainStickySprite : subStickySprite;
                if(sprite != null)
                    sticky.sprite = sprite;

                TMP_Text label = sticky.GetComponentInChildren<TMP_Text>(true);
                if(label != null)
                    label.text = Localization.Resolve(notes[i].text);
                sticky.gameObject.SetActive(true);
            }
            for(int i = notes.Count; i < _stickies.Count; i++)
                _stickies[i].gameObject.SetActive(false);
        }

        private static List<NotebookEntryView> BuildViews(NotebookTab tab)
        {
            if(tab == NotebookTab.Quests)
                return NotebookPageBuilder.BuildQuestList(DataManager.Instance.GetRows<QuestData>(), QuestLog.GetState);

            var notes = new List<NotebookData>();
            foreach(NotebookData note in DataManager.Instance.GetRows<NotebookData>())
            {
                if(IsInTab(note.category, tab))
                    notes.Add(note);
            }
            return NotebookPageBuilder.BuildNoteList(notes, NotebookLog.GetState, Localization.Resolve("@ui.notebook.case"));
        }

        private static bool IsInTab(string category, NotebookTab tab)
        {
            switch(tab)
            {
                case NotebookTab.Residents:
                    return category == NotebookData.Profile;
                case NotebookTab.Alibis:
                    return category == NotebookData.Alibi;
                case NotebookTab.Questions:
                    return category == NotebookData.Question;
                case NotebookTab.Documents:
                    return category == NotebookData.Document || category == NotebookData.Todo;
                default:
                    return false;
            }
        }

        private static string TabTitleKey(NotebookTab tab)
        {
            return "@ui.notebook.tab." + tab.ToString().ToLowerInvariant();
        }

        private void RefreshEntries()
        {
            if(entryTemplate == null || entryContainer == null)
                return;

            for(int i = 0; i < _views.Count; i++)
            {
                NotebookEntryView view = _views[i];
                Button entry = GetOrCreate(_entries, entryTemplate, entryContainer, i);
                entry.onClick.RemoveAllListeners();
                entry.interactable = !view.isHeader;
                if(!view.isHeader)
                {
                    int id = view.id;
                    entry.onClick.AddListener(() => Select(id));
                }

                TMP_Text label = entry.GetComponentInChildren<TMP_Text>(true);
                if(label != null)
                    label.text = FormatLabel(view, view.id == _selectedId);
                entry.gameObject.SetActive(true);
            }
            for(int i = _views.Count; i < _entries.Count; i++)
                _entries[i].gameObject.SetActive(false);

            if(emptyText != null)
            {
                emptyText.gameObject.SetActive(_views.Count == 0);
                emptyText.text = Localization.Resolve("@ui.notebook.empty");
            }
        }

        private static string FormatLabel(NotebookEntryView view, bool selected)
        {
            string text = Localization.Resolve(view.label);
            if(view.isHeader)
                return $"<b>{text}</b>";
            if(view.isStruck || view.isDone)
                text = $"<s>{text}</s>";
            if(view.isUnread)
                text = $"<color={UnreadColor}>{text}</color>";
            return selected ? $"<u>{text}</u>" : text;
        }

        private void Select(int id)
        {
            _selectedId = id;
            ShowDetail(id);
            if(_tab != NotebookTab.Quests)
                NotebookLog.MarkRead(id);
            _dirty = true;
        }

        private void ShowDetail(int id)
        {
            string title = string.Empty;
            string body = string.Empty;

            if(id > 0 && _tab == NotebookTab.Quests)
            {
                QuestData quest = DataManager.Instance.GetRow<QuestData>(id);
                if(quest != null)
                {
                    title = Localization.Resolve(quest.title);
                    body = BuildQuestBody(quest);
                }
            }
            else if(id > 0)
            {
                NotebookData note = DataManager.Instance.GetRow<NotebookData>(id);
                if(note != null)
                {
                    title = Localization.Resolve(note.title);
                    body = Localization.Resolve(note.body);
                }
            }

            if(detailTitle != null)
                detailTitle.text = title;
            if(detailBody != null)
                detailBody.text = body;
        }

        private static string BuildQuestBody(QuestData quest)
        {
            List<QuestObjectiveData> objectives = QuestLog.GetObjectives(quest);
            bool questDone = QuestLog.GetState(quest.dataId) == QuestState.Done;
            var texts = new List<string>(objectives.Count);
            var done = new List<bool>(objectives.Count);
            foreach(QuestObjectiveData objective in objectives)
            {
                texts.Add(Localization.Resolve(objective.text));
                done.Add(questDone || QuestLog.IsObjectiveDone(objective));
            }
            return NotebookPageBuilder.BuildQuestDetail(Localization.Resolve(quest.summary), texts, done,
                QuestLog.GetProgress(quest), Localization.Resolve("@ui.notebook.progress"));
        }

        private bool ContainsSelectable(int id)
        {
            foreach(NotebookEntryView view in _views)
            {
                if(!view.isHeader && view.id == id)
                    return true;
            }
            return false;
        }

        private int FirstSelectableId()
        {
            foreach(NotebookEntryView view in _views)
            {
                if(!view.isHeader)
                    return view.id;
            }
            return 0;
        }

        private static T GetOrCreate<T>(List<T> pool, T template, Transform parent, int index) where T : Component
        {
            while(pool.Count <= index)
                pool.Add(Instantiate(template, parent));
            return pool[index];
        }

        public override bool OnCancel()
        {
            OnClose();
            return true;
        }

        private void OnClose()
        {
            UIManager.Instance.Close<NotebookUI>();
        }
    }
}
