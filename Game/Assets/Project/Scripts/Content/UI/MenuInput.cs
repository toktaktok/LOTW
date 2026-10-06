using UnityEngine;
using UnityEngine.InputSystem;
using Project.Scripts.Content.Story;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.Framework;
using Project.Scripts.Framework.Managers;
using Project.Scripts.Framework.UI;

namespace Project.Scripts.Content.UI
{
    /// <summary>
    /// 메뉴 입력 한 곳에서 처리: Notebook(Q) 수첩 열고 닫기, Pause(Esc) 일시정지, UI/Cancel 로 최상단 창 닫기.
    /// 최상단 창이 있으면 Esc/취소는 그 창의 BaseUI.OnCancel 로 가고, 없으면 게임플레이(HUD 열림) 중에만 일시정지를 엽니다.
    /// 시퀀스, 미니게임, 씬 전환 중에는 아무것도 하지 않습니다. 게임 시작 시 스스로 만들어집니다.
    /// </summary>
    public class MenuInput : Singleton<MenuInput>
    {
        private PlayerControls _controls;

        /// <summary>HUD 수첩 버튼에 표시할 키 이름 (키보드 바인딩).</summary>
        public string NotebookKeyLabel => _controls.Player.Notebook.GetBindingDisplayString(InputBinding.MaskByGroup(UIDefines.KeyboardControlScheme));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            _ = Instance;
        }

        protected override void Awake()
        {
            base.Awake();
            if(Instance != this)
                return;

            _controls = new PlayerControls();
        }

        private void OnEnable()
        {
            _controls?.Player.Notebook.Enable();
            _controls?.Player.Pause.Enable();
            _controls?.UI.Cancel.Enable();
        }

        private void OnDisable()
        {
            _controls?.Player.Notebook.Disable();
            _controls?.Player.Pause.Disable();
            _controls?.UI.Cancel.Disable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _controls?.Dispose();
        }

        private void Update()
        {
            // 중복 인스턴스는 Destroy 가 프레임 끝에 처리되므로 그 사이 한 번 들어올 수 있음
            if(_controls == null)
                return;

            bool pause = _controls.Player.Pause.WasPressedThisFrame();
            bool cancel = _controls.UI.Cancel.WasPressedThisFrame();
            bool notebook = _controls.Player.Notebook.WasPressedThisFrame();
            if(!pause && !cancel && !notebook)
                return;
            if(IsBusy())
                return;

            BaseUI top = UIManager.Instance.GetTopUI();
            if(pause || cancel)
            {
                if(top != null)
                    top.OnCancel();
                else if(pause && CanOpenGameMenu())
                    UIManager.Instance.PushPage<PauseUI>(UILayer.System);
                return;
            }

            if(top is NotebookUI)
                top.OnCancel();
            else if(top == null && CanOpenGameMenu())
                UIManager.Instance.PushPage<NotebookUI>(UILayer.Popup);
        }

        // 미니게임은 자체 입력(Esc 없음)을 쓰고, 시퀀스/전환 중에는 창을 열거나 닫지 않음
        private static bool IsBusy()
        {
            if(UIManager.Instance.IsProcessing || SequencePlayer.IsPlaying)
                return true;
            if(MinigameManager.HasInstance && MinigameManager.Instance.IsPlaying)
                return true;
            return SceneTransitionManager.HasInstance && SceneTransitionManager.Instance.IsTransitioning;
        }

        /// <summary>타이틀 화면에서는 HUD 가 없으므로 열리지 않음.</summary>
        private static bool CanOpenGameMenu()
        {
            return UIManager.Instance.IsOpen<HudUI>();
        }
    }
}
