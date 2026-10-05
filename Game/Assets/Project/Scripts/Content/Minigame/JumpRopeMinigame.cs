using UnityEngine;
using Project.Scripts.System.Minigame;

namespace Project.Scripts.Content.Minigame
{
    /// <summary>
    /// 줄넘기 메카닉. 도는 줄이 바닥을 지나는 순간에 맞춰 Submit으로 뛰어 넘습니다.
    /// 넘으면 jumps, 걸리면 miss 변수를 올리고, 성공/실패 기준은 정의의 outcomes가 정합니다.
    /// </summary>
    public class JumpRopeMinigame : MinigameBase<JumpRopeDefinition>
    {
        private const string JumpsVar = "jumps";
        private const string MissVar = "miss";
        private const float FullTurn = Mathf.PI * 2f;

        [Header("Stage")]
        [SerializeField] private Transform jumper;
        [SerializeField] private LineRenderer rope;
        [Tooltip("줄 양 끝(손잡이) 위치. 스테이지 로컬 좌표")]
        [SerializeField] private Vector2 handlePosition = new Vector2(3.6f, 0.2f);
        [Tooltip("줄이 손잡이 높이에서 위아래로 늘어지는 폭. 손잡이 높이 - 이 값 = 바닥")]
        [SerializeField] private float ropeSag = 2.2f;

        private Vector3 _jumperGround;
        private float _phase;
        private float _period;
        private float _jumpTime = -1f;

        private void Awake()
        {
            _jumperGround = jumper.localPosition;
            // 줄이 머리 위에 있는 상태로 시작
            _phase = Mathf.PI;
            UpdateRope();
        }

        public override void OnBegin()
        {
            _period = Definition.StartPeriod;
            UpdateStatus();
        }

        public override void OnTick(float deltaTime)
        {
            if(_jumpTime < 0f && Session.Input.SubmitPressed)
                _jumpTime = 0f;

            float height = UpdateJump(deltaTime);

            float previous = _phase;
            _phase += FullTurn * deltaTime / _period;
            // 한 바퀴를 넘어가는 순간 = 줄이 바닥을 지나는 순간
            if(Mathf.FloorToInt(previous / FullTurn) != Mathf.FloorToInt(_phase / FullTurn))
                OnRopePassed(height);

            UpdateRope();
        }

        private float UpdateJump(float deltaTime)
        {
            float height = 0f;
            if(_jumpTime >= 0f)
            {
                _jumpTime += deltaTime;
                float t = _jumpTime / Definition.JumpDuration;
                if(t >= 1f)
                    _jumpTime = -1f;
                else
                    height = 4f * Definition.JumpHeight * t * (1f - t);
            }

            jumper.localPosition = _jumperGround + Vector3.up * height;
            return height;
        }

        private void OnRopePassed(float height)
        {
            if(height >= Definition.ClearHeight)
            {
                Session.Vars.Add(JumpsVar);
                _period = Mathf.Max(Definition.MinPeriod, _period - Definition.PeriodStep);
            }
            else
                Session.Vars.Add(MissVar);

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            Session.SetStatus($"넘기 {Session.Vars.Get(JumpsVar)}  걸림 {Session.Vars.Get(MissVar)}");
        }

        // 줄을 옆에서 본 모습: 양 끝은 고정, 가운데가 위(머리 위)와 아래(바닥) 사이를 오감
        private void UpdateRope()
        {
            float swing = Mathf.Cos(_phase);
            int count = rope.positionCount;
            for(int i = 0; i < count; i++)
            {
                float s = i / (float)(count - 1);
                float x = Mathf.Lerp(-handlePosition.x, handlePosition.x, s);
                float y = handlePosition.y - ropeSag * Mathf.Sin(Mathf.PI * s) * swing;
                rope.SetPosition(i, new Vector3(x, y, 0f));
            }
        }
    }
}
