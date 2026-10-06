using System;
using UnityEngine;
using Project.Scripts.Data;

namespace Project.Scripts.System.Minigame
{
    /// <summary>
    /// 진행 중인 미니게임 한 판. 메카닉은 이 객체로 변수/입력을 다루고 종료를 요청합니다.
    /// 종료 판정과 보상은 MinigameManager가 처리합니다.
    /// </summary>
    public class MinigameSession
    {
        public MinigameDefinition Definition { get; }
        /// <summary>미니게임을 연 월드 오브젝트 (없을 수 있음).</summary>
        public GameObject Source { get; }
        public MinigameVars Vars { get; } = new();
        public MinigameInput Input { get; }
        /// <summary>이번 판의 스테이지 RT 해상도. SourceBounds 배치면 발생원 영역 크기, 아니면 정의의 값.</summary>
        public Vector2Int StageResolution { get; }
        public float Elapsed { get; private set; }
        public string Status { get; private set; }

        /// <summary>메카닉이 End(name)으로 요청한 결과 이름. 요청이 없으면 null.</summary>
        public string RequestedOutcome { get; private set; }
        public bool IsAbortRequested { get; private set; }

        public event Action<string> OnStatusChanged;

        public MinigameSession(MinigameDefinition definition, GameObject source, MinigameInput input, Vector2Int stageResolution)
        {
            Definition = definition;
            Source = source;
            Input = input;
            StageResolution = stageResolution;
        }

        /// <summary>정의의 outcomes 중 이름이 같은 결과로 끝냅니다 (when 조건 없이 확정).</summary>
        public void End(string outcomeName) => RequestedOutcome = outcomeName;

        /// <summary>보상 없이 중단합니다 (정의의 abortActions 실행).</summary>
        public void Abort() => IsAbortRequested = true;

        /// <summary>진행 상황 문구. 지금은 창에 표시하지 않음 (제목 줄 제거, 표시 위치 미정).</summary>
        public void SetStatus(string status)
        {
            if(Status == status)
                return;

            Status = status;
            OnStatusChanged?.Invoke(status);
        }

        public void Tick(float deltaTime)
        {
            Elapsed += deltaTime;
            Vars.Set(MinigameDefines.TimeVar, Mathf.FloorToInt(Elapsed));
        }
    }
}
