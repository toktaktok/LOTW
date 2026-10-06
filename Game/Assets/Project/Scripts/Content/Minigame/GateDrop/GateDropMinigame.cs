using UnityEngine;
using Project.Scripts.Core;
using Project.Scripts.Data;
using Project.Scripts.System.Minigame;

namespace Project.Scripts.Content.Minigame.GateDrop
{
    /// <summary>
    /// 낙하 게이트 메카닉. 캔이 투입구에서 떨어져 핀을 타고 튕기며 내려가고, 맨 아래 출구 하나에 들어갑니다.
    /// 버튼을 누르면 그 레인의 게이트가 잠깐 열립니다. 캔이 열린 게이트에 닿으면 그 출구로 바로 떨어지고,
    /// 닫힌 게이트에 닿으면 핀처럼 튕깁니다. 캔이 들어간 출구의 outcome으로 끝납니다.
    /// 판 크기는 Session.StageResolution(자판기가 화면에서 차지하는 영역)에 맞춰 OnBind에서 만듭니다.
    /// </summary>
    public class GateDropMinigame : MinigameBase<GateDropDefinition>
    {
        private const string OutletVar = "outlet";
        private const string PressesVar = "presses";
        private const float NavigateThreshold = 0.5f;
        /// <summary>투입구에서 첫 핀까지 떨어지는 시간 = 첫 줄 시간 x 이 값.</summary>
        private const float InletDropScale = 1.1f;
        /// <summary>열린 게이트로 빠져 출구까지 떨어지는 시간.</summary>
        private const float CatchDuration = 0.16f;
        /// <summary>마지막 줄 핀에서 출구로 튀는 높이 배율과 시간 배율.</summary>
        private const float PocketHopHeight = 0.55f;
        private const float PocketHopTime = 1.2f;
        /// <summary>아래 줄로 갈수록 튀는 높이가 이 비율까지 줄어듦.</summary>
        private const float BottomHopHeight = 0.55f;
        private const float HopJitter = 0.15f;
        /// <summary>벽에 부딪히는 경로: 벽까지 가는 시간 비율.</summary>
        private const float WallSplit = 0.45f;
        private const float RattleInterval = 0.14f;
        /// <summary>달그락 간격이 이 비율만큼 무작위로 늘거나 줆.</summary>
        private const float RattleJitter = 0.3f;
        private const float RattleDegrees = 18f;
        /// <summary>투입구에서 캔이 좌우로 떨리는 속도(rad/s)와 폭(px, 반올림 전).</summary>
        private const float InletWobbleSpeed = 37f;
        private const float InletWobbleAmount = 0.7f;
        private static readonly float[] SettleHeights = { 3f, 1.3f, 0.5f };
        private static readonly float[] SettleDurations = { 0.13f, 0.09f, 0.06f };
        /// <summary>캔 찌그러짐 세기 (GateDropCan.Impact).</summary>
        private const float PegImpact = 1f;
        private const float WallImpact = 0.8f;
        private const float CatchImpact = 0.6f;
        private const float LandImpact = 1.4f;
        private const float CaughtLandImpact = 1.8f;
        private const float SettleImpact = 0.5f;
        private const float RevealImpact = 0.7f;
        /// <summary>판 흔들림 시간(초). 폭은 ShakePixels.</summary>
        private const float GateBumpShake = 0.06f;
        private const float LandShake = 0.16f;
        private const int ShakePixels = 1;
        /// <summary>포인터가 이만큼(화면 픽셀 거리 제곱) 움직여야 레인을 고름.</summary>
        private const float PointerMoveEpsilon = 0.25f;
        private const int ConfettiCount = 18;
        private const int PuffCount = 10;
        private const float BackgroundBrightness = 0.4f;

        [Header("Stage")]
        [Tooltip("판 스프라이트에 쓸 머티리얼 (URP Sprite-Unlit-Default)")]
        [SerializeField] private Material spriteMaterial;

        private GateDropLayout _layout;
        private GateDropArt _art;
        private GateDropView _view;
        private GateDropSound _sound;
        private GateDropPhase _phase;
        private float _phaseTime;

        private GateDropHop _hop;
        private GateDropHop _nextHop;
        private bool _hasNextHop;
        private int _wallDirection;
        // 지금 뛰어가는 목표 핀. 출구로 떨어지는 중이면 _targetLane >= 0
        private int _row;
        private int _column;
        private int _targetLane = -1;
        private bool _caught;
        private int _settleIndex;

        private int _selected;
        private int _openLane = -1;
        private int _navigateDirection;
        private Vector2 _lastPointer;
        private bool _hasPointer;
        private float _rattleTime;

        #region Unity Lifecycle

        private void Update()
        {
            _view?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _art?.Release();
            _sound?.Release();
        }

        #endregion

        #region Minigame

        protected override void OnBind()
        {
            if(Definition == null)
            {
                Debug.LogError($"[GateDropMinigame] '{Session.Definition.Id}' is not a GateDropDefinition.");
                return;
            }

            GateDropOutlet[] outlets = Definition.Outlets;
            int count = outlets != null ? outlets.Length : 0;
            if(count < GateDropDefines.MinOutlets || count > GateDropDefines.MaxOutlets)
            {
                Debug.LogError($"[GateDropMinigame] '{Definition.Id}' needs {GateDropDefines.MinOutlets}-{GateDropDefines.MaxOutlets} outlets (has {count}).");
                return;
            }

            _layout = new GateDropLayout(Session.StageResolution, count);
            _art = new GateDropArt(_layout, Definition);
            _sound = new GateDropSound();
            _view = new GateDropView(transform, _layout, _art, Definition, spriteMaterial);

            // 흔들 때 드러나는 가장자리가 판 테두리와 같은 색이 되게
            if(StageCamera != null)
            {
                Color board = Definition.BoardColor * BackgroundBrightness;
                board.a = 1f;
                StageCamera.backgroundColor = board;
            }

            _selected = (count - 1) / 2;
            _view.SetCursor(_selected, true);
            _view.Can.SetPosition(_layout.Inlet, 0f);
            _phase = GateDropPhase.Idle;
            UpdateStatus();
        }

        public override void OnBegin()
        {
            if(_layout == null)
            {
                Session.Abort();
                return;
            }

            _phase = GateDropPhase.Ready;
            _phaseTime = 0f;
        }

        public override void OnTick(float deltaTime)
        {
            if(_layout == null)
                return;

            if(_phase == GateDropPhase.Ready || _phase == GateDropPhase.Falling)
                HandleInput();

            _phaseTime += deltaTime;
            switch(_phase)
            {
                case GateDropPhase.Ready:
                    TickReady(deltaTime);
                    break;
                case GateDropPhase.Falling:
                    TickFalling(deltaTime);
                    break;
                case GateDropPhase.Landing:
                    TickLanding(deltaTime);
                    break;
            }
        }

        #endregion

        #region Input

        private void HandleInput()
        {
            MinigameInput input = Session.Input;

            float x = input.Navigate.x;
            int direction = x > NavigateThreshold ? 1 : x < -NavigateThreshold ? -1 : 0;
            if(direction != 0 && direction != _navigateDirection)
                Select(Mathf.Clamp(_selected + direction, 0, _layout.Lanes - 1));
            _navigateDirection = direction;

            if(input.SubmitPressed)
                Press(_selected);

            HandlePointer(input);
        }

        // 포인터는 움직였을 때만 고름 (가만히 있는 포인터가 키보드 선택을 덮지 않게).
        // 처음 잡힌 프레임은 위치만 기억 (창이 열릴 때 커서 아래 레인이 저절로 골라지지 않게).
        // 움직임은 화면 좌표로 잼 (창이 발생원을 따라 움직여도 가만히 있는 포인터는 움직이지 않은 것)
        private void HandlePointer(MinigameInput input)
        {
            Vector2 screen = input.PointerScreen;
            if(!input.TryScreenToWorld(screen, out Vector3 world))
            {
                _hasPointer = false;
                return;
            }

            bool moved = _hasPointer && (screen - _lastPointer).sqrMagnitude > PointerMoveEpsilon;
            _lastPointer = screen;
            _hasPointer = true;

            int lane = _layout.GetLaneAt(_layout.ToPixel(transform.InverseTransformPoint(world)));
            if(lane < 0)
                return;

            if(moved)
                Select(lane);
            if(input.ClickPressed)
            {
                Select(lane);
                Press(lane);
            }
        }

        private void Select(int lane)
        {
            if(lane == _selected)
                return;

            _selected = lane;
            _view.SetCursor(lane, false);
            _sound.PlayCursor();
            UpdateStatus();
        }

        // 버튼을 누르면 그 레인 게이트가 열리고, 먼저 열려 있던 다른 게이트는 닫힘
        private void Press(int lane)
        {
            Session.Vars.Add(PressesVar);
            if(_openLane >= 0 && _openLane != lane)
                _view.Gates[_openLane].Close();

            _openLane = lane;
            _view.Buttons[lane].Press();
            _view.Gates[lane].Open(Definition.GateOpenTime);
            _view.Link(lane, Definition.Outlets[lane].color);
            _sound.PlayButton();
            _sound.PlayGateOpen();
        }

        #endregion

        #region Ready

        // 투입구에서 캔이 달그락거리다가 떨어짐
        private void TickReady(float deltaTime)
        {
            float wobble = Mathf.Round(Mathf.Sin(_phaseTime * InletWobbleSpeed) * InletWobbleAmount);
            _view.Can.SetPosition(_layout.Inlet + new Vector2(wobble, 0f), 0f);

            _rattleTime -= deltaTime;
            if(_rattleTime <= 0f)
            {
                _rattleTime = RattleInterval * Random.Range(1f - RattleJitter, 1f + RattleJitter);
                _view.Can.Jiggle(Random.value < 0.5f ? -RattleDegrees : RattleDegrees);
                _sound.PlayRattle();
            }

            if(_phaseTime < Definition.StartDelay)
                return;

            GateDropBoard board = _layout.Board;
            _row = 0;
            _column = board.StartColumn;
            StartHop(new GateDropHop(_layout.Inlet, _layout.GetContact(_row, _column), 0f, Definition.FirstHopDuration * InletDropScale));
            _sound.PlayDrop();
            SetPhase(GateDropPhase.Falling);
        }

        #endregion

        #region Falling

        private void TickFalling(float deltaTime)
        {
            _hop.Elapsed += deltaTime;
            float lift = _hop.Height > 0f ? _hop.Lift : 0f;
            _view.Can.SetPosition(_hop.Position, lift);
            if(!_hop.IsDone)
                return;

            if(_hasNextHop)
            {
                // 벽에 닿음: 튕겨서 남은 길을 감
                _hasNextHop = false;
                _view.Can.Impact(-_wallDirection, WallImpact);
                _view.Particles.Sparks(_hop.To + new Vector2(_wallDirection * (_layout.CanRadius + 1), 0f), -_wallDirection);
                _sound.PlayWall();
                StartHop(_nextHop);
                return;
            }

            if(_targetLane >= 0)
                BeginLanding(_targetLane);
            else
                OnContact();
        }

        // 캔이 (_row, _column) 핀이나 게이트에 닿음
        private void OnContact()
        {
            GateDropBoard board = _layout.Board;
            if(board.IsGate(_row, _column))
            {
                int lane = GateDropBoard.GetLane(_column);
                GateDropGate gate = _view.Gates[lane];
                if(gate.IsOpen)
                {
                    Catch(lane);
                    return;
                }
                gate.Bump();
                _view.Shake(GateBumpShake, ShakePixels);
                _sound.PlayGateBump();
            }
            else
                _sound.PlayPeg(_row / (float)Mathf.Max(1, board.LastRow));

            bool right = Random.value < Definition.PegRightChance;
            int direction = right ? 1 : -1;
            _view.HitPeg(_row, _column, direction);
            _view.Can.Impact(direction, PegImpact);
            Vector2 contact = new Vector2(_layout.ColumnX(_column), _layout.RowY(_row) + GateDropDefines.PegSize / 2 + 1);
            _view.Particles.Sparks(contact, direction);

            int next = board.Bounce(_column, right, out bool hitWall);
            Vector2 from = _layout.GetContact(_row, _column);
            float progress = _row / (float)Mathf.Max(1, board.LastRow);
            float height = Definition.HopHeight * Mathf.Lerp(1f, BottomHopHeight, progress) * Random.Range(1f - HopJitter, 1f + HopJitter);
            float duration = Mathf.Lerp(Definition.FirstHopDuration, Definition.LastHopDuration, progress);

            if(_row == board.LastRow)
            {
                // 칸막이 위 핀에서 양옆 출구 중 하나로
                _targetLane = GateDropBoard.GetLane(next);
                StartHop(new GateDropHop(from, _layout.GetPocket(_targetLane), height * PocketHopHeight, duration * PocketHopTime));
                return;
            }

            _row++;
            _column = next;
            Vector2 to = _layout.GetContact(_row, _column);
            if(hitWall)
                StartWallHop(from, to, direction, height, duration);
            else
                StartHop(new GateDropHop(from, to, height, duration));
        }

        // 벽 쪽으로 튀었다가 벽에 맞고 안쪽 다음 핀으로 떨어지는 두 구간
        private void StartWallHop(Vector2 from, Vector2 to, int direction, float height, float duration)
        {
            float wallX = direction < 0 ? _layout.Left + 1 + _layout.CanRadius : _layout.Right - 1 - _layout.CanRadius;
            Vector2 wall = new Vector2(wallX, Mathf.Lerp(from.y, to.y, WallSplit));
            _wallDirection = direction;
            _nextHop = new GateDropHop(wall, to, 0f, duration * (1f - WallSplit));
            _hasNextHop = true;
            StartHop(new GateDropHop(from, wall, height, duration * WallSplit));
        }

        // 열린 게이트로 캔이 빠짐: 문짝이 한 번 더 흔들리고 출구로 곧장 떨어짐
        private void Catch(int lane)
        {
            _caught = true;
            _targetLane = lane;
            _view.Gates[lane].Pass();
            _view.Can.Impact(0, CatchImpact);
            _sound.PlayDrop();
            StartHop(new GateDropHop(_layout.GetContact(_row, _column), _layout.GetPocket(lane), 0f, CatchDuration));
        }

        private void StartHop(GateDropHop hop)
        {
            _hop = hop;
            _view.Can.SetPosition(hop.From, 0f);
        }

        #endregion

        #region Landing

        private void BeginLanding(int lane)
        {
            SetPhase(GateDropPhase.Landing);
            Session.Vars.Set(OutletVar, lane + 1);
            if(_openLane >= 0)
                _view.Gates[_openLane].Close();

            _settleIndex = 0;
            Vector2 pocket = _layout.GetPocket(lane);
            _hop = new GateDropHop(pocket, pocket, SettleHeights[0], SettleDurations[0]);

            GateDropPocket target = _view.Pockets[lane];
            target.HideSample();
            target.Glow();
            _view.HideCursor();
            _view.Can.Impact(0, _caught ? CaughtLandImpact : LandImpact);
            _view.Shake(LandShake, ShakePixels);
            _sound.PlayLand();
            _selected = lane;
            UpdateStatus();
        }

        // 출구 바닥에서 작게 세 번 튕기고 멈춘 뒤 음료 색이 차오름. landDuration이 지나면 결과
        private void TickLanding(float deltaTime)
        {
            if(_settleIndex < SettleHeights.Length)
            {
                _hop.Elapsed += deltaTime;
                _view.Can.SetPosition(_hop.Position, _hop.Lift);
                if(_hop.IsDone)
                {
                    _settleIndex++;
                    if(_settleIndex < SettleHeights.Length)
                    {
                        _view.Can.Impact(0, SettleImpact);
                        _sound.PlaySettle();
                        _hop = new GateDropHop(_hop.To, _hop.To, SettleHeights[_settleIndex], SettleDurations[_settleIndex]);
                    }
                    else
                        Reveal();
                }
            }

            if(_phaseTime < Definition.LandDuration)
                return;

            SetPhase(GateDropPhase.Done);
            Session.End(Definition.Outlets[_targetLane].outcome);
        }

        private void Reveal()
        {
            GateDropOutlet outlet = Definition.Outlets[_targetLane];
            Vector2 pocket = _layout.GetPocket(_targetLane);
            _view.Can.Flood(outlet.color);
            _view.Can.Impact(0, RevealImpact);
            if(outlet.dud)
            {
                _view.Particles.Puff(pocket, outlet.color, PuffCount);
                _sound.PlayDud();
            }
            else
            {
                _view.Particles.Confetti(pocket + new Vector2(0f, _layout.CanRadius), outlet.color, ConfettiCount);
                _sound.PlayWin();
            }
        }

        #endregion

        private void SetPhase(GateDropPhase phase)
        {
            _phase = phase;
            _phaseTime = 0f;
        }

        private void UpdateStatus()
        {
            Session.SetStatus(Localization.Resolve(Definition.Outlets[_selected].label));
        }
    }
}
