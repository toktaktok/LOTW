using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Project.Scripts.Content.Story;
using Project.Scripts.Content.Title;
using Project.Scripts.Core;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.System.UI;

namespace Project.Scripts.Content.UI
{
    /// <summary>
    /// 메인 화면: 사무소 서류가방 안의 종이 4장 (이어하기 / 새로하기 / 설정 / 종료).
    /// 이어하기는 가장 최근 세이브를 불러오고, 세이브가 없으면 잠깁니다.
    /// </summary>
    public class TitleUI : BaseUI
    {
        [SerializeField] private TitleSheet continueSheet;
        [SerializeField] private TitleSheet newGameSheet;
        [SerializeField] private TitleSheet settingsSheet;
        [SerializeField] private TitleSheet quitSheet;
        [Tooltip("이어하기 종이 아래 줄: 장소 / 플레이 시간")]
        [SerializeField] private TMP_Text continueInfoText;

        private int _latestSlot = -1;

        protected override void Awake()
        {
            base.Awake();
            if(continueSheet != null)
                continueSheet.Button.onClick.AddListener(OnContinue);
            if(newGameSheet != null)
                newGameSheet.Button.onClick.AddListener(OnNewGame);
            if(settingsSheet != null)
                settingsSheet.Button.onClick.AddListener(() => UIManager.Instance.PushPage<SettingsUI>(UILayer.System));
            if(quitSheet != null)
                quitSheet.Button.onClick.AddListener(() => UIManager.Instance.PushPage<ConfirmUI>(UILayer.System, ui => ui.Setup("@ui.title.quit.confirm", Quit)));
        }

        private void OnEnable()
        {
            Refresh();
        }

        // 설정/확인 팝업이 닫힌 뒤 선택이 사라진 채 남지 않도록 키보드 포커스를 되돌림
        private void Update()
        {
            // 위에 창이 열리는 중(페이드)에는 아직 이 UI 가 최상단이라 그 창의 선택을 뺏지 않도록 처리 중이면 건너뜀
            if(EventSystem.current == null || UIManager.Instance.IsProcessing || UIManager.Instance.GetTopUI() != this)
                return;

            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if(selected == null || !selected.activeInHierarchy || !selected.transform.IsChildOf(transform))
                SelectDefault();
        }

        private void Refresh()
        {
            var slots = new List<SaveData?>(SaveDefines.MaxSlots);
            for(int i = 0; i < SaveDefines.MaxSlots; i++)
                slots.Add(SaveManager.Instance.PeekSave(i));
            _latestSlot = SaveSlotSelector.FindLatest(slots);

            bool canContinue = _latestSlot >= 0;
            if(continueSheet != null)
                continueSheet.Button.interactable = canContinue;
            if(newGameSheet != null)
            {
                // 명시적 이동은 interactable 을 무시하므로 이어하기가 잠기면 왼쪽 이동을 끊음
                Navigation nav = newGameSheet.Button.navigation;
                nav.selectOnLeft = canContinue && continueSheet != null ? continueSheet.Button : null;
                newGameSheet.Button.navigation = nav;
            }
            if(continueInfoText != null)
                continueInfoText.text = canContinue ? FormatInfo(slots[_latestSlot].Value) : string.Empty;

            SelectDefault();
        }

        private void SelectDefault()
        {
            if(EventSystem.current == null)
                return;

            bool canContinue = continueSheet != null && continueSheet.Button.interactable;
            TitleSheet first = canContinue ? continueSheet : newGameSheet;
            if(first != null)
                EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        private static string FormatInfo(SaveData data)
        {
            string place = Localization.Resolve("@world.scene." + data.currentScene.ToLowerInvariant());
            return $"{place}  {SaveSlotSelector.FormatPlayTime(data.playTime)}";
        }

        private void OnContinue()
        {
            if(_latestSlot < 0)
                return;

            UIManager.Instance.ClearAllPages();
            SaveManager.Instance.LoadAndApply(_latestSlot);
        }

        private void OnNewGame()
        {
            if(_latestSlot >= 0)
                UIManager.Instance.PushPage<ConfirmUI>(UILayer.System, ui => ui.Setup("@ui.title.newgame.confirm", StartNewGame));
            else
                StartNewGame();
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void StartNewGame()
        {
            int slot = SaveSlotSelector.GetNewGameSlot(GameInstance.Instance.CurrentSaveSlot, SaveDefines.MaxSlots);
            UIManager.Instance.ClearAllPages();
            SaveManager.Instance.NewGame(slot);
            SequencePlayer.Instance.Play(StoryDefines.PrologueSequenceId);
        }
    }
}
