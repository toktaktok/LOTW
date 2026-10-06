using UnityEngine;
using Project.Scripts.Data;

namespace Project.Scripts.Content.Minigame.GateDrop
{
    /// <summary>
    /// 낙하 게이트 미니게임 정의. 캔이 들어간 출구의 outcome 이름으로 끝나므로 outcomes의 when은 비웁니다.
    /// 메카닉이 쓰는 변수: outlet(들어간 출구 번호, 1부터), presses(버튼 누른 횟수).
    /// </summary>
    [CreateAssetMenu(fileName = "MinigameDefinition_GateDrop", menuName = "LOTW/Minigame/Gate Drop Definition")]
    public class GateDropDefinition : MinigameDefinition
    {
        [Header("Gate Drop")]
        [Tooltip("출구 목록 (왼쪽부터). 버튼과 게이트도 이 수만큼 생김. 2~8개")]
        [SerializeField] private GateDropOutlet[] outlets;
        [Tooltip("핀(닫힌 게이트 포함)에서 오른쪽으로 갈 확률")]
        [Range(0f, 1f)]
        [SerializeField] private float pegRightChance = 0.5f;
        [Tooltip("준비 상태 시간(초). 캔이 투입구에서 흔들림")]
        [SerializeField] private float startDelay = 0.6f;
        [Tooltip("버튼을 누르면 게이트가 열려 있는 시간(초). 다른 버튼을 누르면 먼저 열린 게이트는 닫힘")]
        [SerializeField] private float gateOpenTime = 0.45f;

        [Header("Feel")]
        [Tooltip("첫 줄에서 한 줄 내려가는 시간(초)")]
        [SerializeField] private float firstHopDuration = 0.3f;
        [Tooltip("마지막 줄에서 한 줄 내려가는 시간(초). 줄마다 이 값으로 빨라짐")]
        [SerializeField] private float lastHopDuration = 0.15f;
        [Tooltip("핀에 맞고 튀어 오르는 높이(스테이지 픽셀). 아래로 갈수록 줄어듦")]
        [SerializeField] private float hopHeight = 3.5f;
        [Tooltip("출구에 들어간 뒤 결과를 내기까지의 시간(초)")]
        [SerializeField] private float landDuration = 0.8f;

        [Header("Colors")]
        [SerializeField] private Color boardColor = new Color(0.16f, 0.29f, 0.31f);
        [Tooltip("떨어지는 캔 윗면 색 (착지 전)")]
        [SerializeField] private Color canColor = new Color(0.78f, 0.8f, 0.83f);

        public GateDropOutlet[] Outlets => outlets;
        public float PegRightChance => pegRightChance;
        public float StartDelay => startDelay;
        public float GateOpenTime => gateOpenTime;
        public float FirstHopDuration => firstHopDuration;
        public float LastHopDuration => lastHopDuration;
        public float HopHeight => hopHeight;
        public float LandDuration => landDuration;
        public Color BoardColor => boardColor;
        public Color CanColor => canColor;

        public override Vector2Int MinStageResolution
        {
            get
            {
                Vector2Int board = GateDropLayout.GetMinimumSize(outlets != null ? outlets.Length : GateDropDefines.MinOutlets);
                return Vector2Int.Max(board, MinigameDefines.MinStageResolution);
            }
        }
    }
}
