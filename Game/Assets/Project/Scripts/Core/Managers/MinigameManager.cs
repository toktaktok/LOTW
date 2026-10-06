using System;
using UnityEngine;
using Project.Scripts.Content.Dialogue;
using Project.Scripts.Content.UI;
using Project.Scripts.Data;
using Project.Scripts.System.Dialogue;
using Project.Scripts.System.Minigame;

namespace Project.Scripts.Core.Managers
{
    /// <summary>
    /// 미니게임 한 판의 수명을 관리합니다. 동시에 한 판만 진행합니다.
    /// Play: 스테이지(메카닉 프리팹)와 RT를 만들고 MinigameWindow를 Popup으로 엽니다.
    /// 진행: 메카닉을 틱하고 정의의 outcomes 를 위에서부터 검사합니다.
    /// 종료: 결과 액션/기록 플래그를 적용하고 창을 닫은 뒤 스테이지를 정리하고 onEnded를 부릅니다.
    /// 창이 Popup 페이지이므로 진행 중에는 UIManager.HasBlockingPage로 플레이어 조작이 막힙니다.
    /// </summary>
    public class MinigameManager : Singleton<MinigameManager>
    {
        [Tooltip("대화 액션 minigame:id 가 찾는 정의 목록")]
        [SerializeField] private MinigameLibrary library;

        private readonly IDialogueContext _gameContext = new ManagerDialogueContext();

        private MinigameSession _session;
        private MinigameContext _context;
        private MinigameBase _mechanic;
        private MinigameInput _input;
        private RenderTexture _stageTexture;
        private Action<MinigameResult> _onEnded;
        private MinigameResult _result;
        // 창이 다 열린 뒤부터 결과 확정 전까지만 true
        private bool _isRunning;

        public bool IsPlaying => _session != null;

        private void Update()
        {
            if(!_isRunning)
                return;

            _session.Tick(Time.deltaTime);
            _mechanic.OnTick(Time.deltaTime);

            if(_session.IsAbortRequested)
                Finish(null);
            else if(_session.RequestedOutcome != null)
                FinishByName(_session.RequestedOutcome);
            else
            {
                MinigameOutcome outcome = MinigameRules.Evaluate(_session.Definition.Outcomes, _context);
                if(outcome != null)
                    Finish(outcome);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Cleanup();
        }

        #region Public API

        public bool TryGetDefinition(string id, out MinigameDefinition definition)
        {
            definition = null;
            if(library != null && library.TryGetDefinition(id, out definition))
                return true;

            Debug.LogWarning($"[MinigameManager] Minigame '{id}' is not in the library.");
            return false;
        }

        /// <summary>시작 조건과 반복 정책을 만족하고 다른 판이 진행 중이 아닌지.</summary>
        public bool CanStart(MinigameDefinition definition)
        {
            return !IsPlaying && MinigameRules.CanStart(definition, _gameContext);
        }

        /// <summary>
        /// 미니게임을 엽니다. 시작할 수 없으면 false. onEnded는 창이 완전히 닫힌 뒤 호출됩니다.
        /// </summary>
        public bool Play(MinigameDefinition definition, GameObject source = null, Action<MinigameResult> onEnded = null)
        {
            if(!CanStart(definition))
                return false;

            if(definition.MechanicPrefab == null || !definition.MechanicPrefab.TryGetComponent(out MinigameBase mechanicPrefab))
            {
                Debug.LogError($"[MinigameManager] '{definition.Id}' has no mechanic prefab with a MinigameBase.");
                return false;
            }

            _input = new MinigameInput();
            _session = new MinigameSession(definition, source, _input, GetStageResolution(definition, source));
            _context = new MinigameContext(_gameContext, _session.Vars);
            _onEnded = onEnded;
            MinigameRules.ApplyStart(definition, _context);

            CreateStage(definition, mechanicPrefab);
            _mechanic.Bind(_session);

            UIManager.Instance.PushPage<MinigameWindow>(UILayer.Popup, window =>
            {
                _input.Bind(window.ScreenRect, _mechanic.StageCamera);
                window.Setup(_session, _stageTexture, OnWindowOpened, Abort, OnWindowClosed);
            });
            return true;
        }

        /// <summary>진행 중인 판을 보상 없이 중단합니다. 정의가 중단을 허용하고 메카닉이 막지 않을 때만 동작합니다.</summary>
        public void Abort()
        {
            if(_isRunning && _session.Definition.AllowAbort && _mechanic.CanAbort)
                Finish(null);
        }

        #endregion

        #region Flow

        // SourceBounds면 발생원이 화면에서 차지하는 영역 크기. 투영할 수 없으면 정의의 해상도.
        private static Vector2Int GetStageResolution(MinigameDefinition definition, GameObject source)
        {
            if(definition.Layout.placement == MinigamePlacement.SourceBounds
                && MinigameProjection.TryGetStageResolution(definition, source, out Vector2Int resolution))
                return resolution;
            return definition.StageResolution;
        }

        private void CreateStage(MinigameDefinition definition, MinigameBase mechanicPrefab)
        {
            Vector2Int resolution = _session.StageResolution;
            _stageTexture = new RenderTexture(resolution.x, resolution.y, MinigameDefines.StageDepthBits)
            {
                filterMode = FilterMode.Point,
                name = $"Minigame_{definition.Id}"
            };

            _mechanic = Instantiate(mechanicPrefab, MinigameDefines.StageOrigin, Quaternion.identity, transform);
            Camera stageCamera = _mechanic.StageCamera;
            if(stageCamera == null)
            {
                Debug.LogError($"[MinigameManager] '{definition.Id}' mechanic prefab has no stage camera.");
                return;
            }

            // 스테이지 1픽셀 = RT 1픽셀이 되도록 맞춤
            stageCamera.orthographicSize = resolution.y * 0.5f / CameraDefines.PixelsPerUnit;
            stageCamera.targetTexture = _stageTexture;
        }

        private void OnWindowOpened()
        {
            _input.Enable();
            _isRunning = true;
            _mechanic.OnBegin();
        }

        private void FinishByName(string outcomeName)
        {
            MinigameOutcome outcome = MinigameRules.Find(_session.Definition.Outcomes, outcomeName);
            if(outcome == null)
                Debug.LogWarning($"[MinigameManager] '{_session.Definition.Id}' has no outcome '{outcomeName}'; treated as aborted.");
            Finish(outcome);
        }

        // 결과 확정: 입력을 끄고 액션/기록을 적용한 뒤 창을 닫음. outcome이 null이면 중단.
        private void Finish(MinigameOutcome outcome)
        {
            ApplyEnd(outcome);
            CloseRoutine(outcome != null ? MinigameDefines.ResultHoldDuration : 0f).Forget();
        }

        private void ApplyEnd(MinigameOutcome outcome)
        {
            _isRunning = false;
            _input.Disable();
            _mechanic.OnEnd(outcome);
            _result = MinigameRules.ApplyEnd(_session.Definition, outcome, _context);
        }

        private async Awaitable CloseRoutine(float holdDuration)
        {
            MinigameSession session = _session;
            if(holdDuration > 0f)
                await Awaitable.WaitForSecondsAsync(holdDuration);

            // 기다리는 동안 창이 밖에서 닫혔으면 이미 정리됨
            if(_session == session)
                UIManager.Instance.PopPage();
        }

        private void OnWindowClosed()
        {
            // 결과 확정 전에 창이 밖에서 닫힌 경우(ClearAllPages 등)는 중단으로 처리
            if(_isRunning)
                ApplyEnd(null);

            Action<MinigameResult> onEnded = _onEnded;
            MinigameResult result = _result;
            Cleanup();
            onEnded?.Invoke(result);
        }

        private void Cleanup()
        {
            _isRunning = false;
            if(_mechanic != null)
            {
                // Destroy는 프레임 끝에 처리되므로, 해제된 RT로 한 번 더 그리지 않게 먼저 끔
                _mechanic.gameObject.SetActive(false);
                Destroy(_mechanic.gameObject);
            }
            if(_stageTexture != null)
            {
                _stageTexture.Release();
                Destroy(_stageTexture);
            }
            _input?.Dispose();

            _mechanic = null;
            _stageTexture = null;
            _input = null;
            _session = null;
            _context = null;
            _onEnded = null;
        }

        #endregion
    }
}
