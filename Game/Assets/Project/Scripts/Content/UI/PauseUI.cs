using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.System.UI;

namespace Project.Scripts.Content.UI
{
    /// <summary>
    /// 일시정지 메뉴: 계속 / 설정 / 타이틀로. 열려 있는 동안 CoreManager 로 게임을 멈춥니다.
    /// 저장은 기획상 사무소 등 저장 지점에서만 하므로 여기에는 없습니다.
    /// </summary>
    public class PauseUI : BaseUI
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button titleButton;

        protected override void Awake()
        {
            base.Awake();
            if(resumeButton != null)
                resumeButton.onClick.AddListener(() => UIManager.Instance.Close<PauseUI>());
            if(settingsButton != null)
                settingsButton.onClick.AddListener(() => UIManager.Instance.PushPage<SettingsUI>(UILayer.System));
            if(titleButton != null)
                titleButton.onClick.AddListener(OnTitle);
        }

        private void OnEnable()
        {
            CoreManager.Instance.PauseGame();
            if(resumeButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }

        private void OnDisable()
        {
            if(CoreManager.HasInstance)
                CoreManager.Instance.ResumeGame();
        }

        public override bool OnCancel()
        {
            UIManager.Instance.Close<PauseUI>();
            return true;
        }

        private void OnTitle()
        {
            UIManager.Instance.PushPage<ConfirmUI>(UILayer.System, ui => ui.Setup("@ui.pause.title.confirm", GoToTitle));
        }

        private static void GoToTitle()
        {
            UIManager.Instance.ClearAllPages();
            SceneTransitionManager.Instance.TransitionTo(SceneDefines.Title);
        }
    }
}
