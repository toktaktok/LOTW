using UnityEngine;
using Project.Scripts.Data;

namespace Project.Scripts.Content.Minigame
{
    /// <summary>
    /// 줄넘기 미니게임 정의. 메카닉이 쓰는 변수: jumps(넘은 횟수), miss(걸린 횟수).
    /// 결과 조건 예: var:jumps>=5 (성공), var:miss>=3 (실패).
    /// </summary>
    [CreateAssetMenu(fileName = "MinigameDefinition_JumpRope", menuName = "LOTW/Minigame/Jump Rope Definition")]
    public class JumpRopeDefinition : MinigameDefinition
    {
        [Header("Jump Rope")]
        [Tooltip("줄이 한 바퀴 도는 시간(초)의 시작값")]
        [SerializeField] private float startPeriod = 1.4f;
        [Tooltip("넘을 때마다 줄어드는 시간(초)")]
        [SerializeField] private float periodStep = 0.06f;
        [SerializeField] private float minPeriod = 0.75f;
        [SerializeField] private float jumpDuration = 0.5f;
        [SerializeField] private float jumpHeight = 1.3f;
        [Tooltip("줄이 바닥을 지날 때 이 높이 이상 떠 있어야 넘은 것으로 침")]
        [SerializeField] private float clearHeight = 0.35f;

        public float StartPeriod => startPeriod;
        public float PeriodStep => periodStep;
        public float MinPeriod => minPeriod;
        public float JumpDuration => jumpDuration;
        public float JumpHeight => jumpHeight;
        public float ClearHeight => clearHeight;
    }
}
