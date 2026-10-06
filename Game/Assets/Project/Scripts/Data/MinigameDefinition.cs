using UnityEngine;

namespace Project.Scripts.Data
{
    /// <summary>
    /// 미니게임 한 판의 규칙 데이터. 메카닉(프리팹)은 "어떻게 노는가"를, 이 에셋은
    /// "언제 열리고, 무엇이 성공/실패이며, 무엇을 주는가"를 정합니다.
    /// 조건/액션 문자열은 대화 테이블과 같은 문법(DialogueCommands)이며, 조건에는 메카닉 변수 var:key 를 쓸 수 있습니다.
    /// 메카닉 전용 수치가 필요하면 이 클래스를 상속합니다 (예: JumpRopeDefinition).
    /// </summary>
    [CreateAssetMenu(fileName = "MinigameDefinition_New", menuName = "LOTW/Minigame/Definition")]
    public class MinigameDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("고유 ID. 대화 액션 minigame:id 와 기록 플래그 mg_{id}_* 에 쓰임")]
        [SerializeField] private string id;
        [Tooltip("창 제목. Text 키 (@키). 원문은 TextKeyReferenceTests 가 막음")]
        [SerializeField] private string title;

        [Header("Stage")]
        [Tooltip("MinigameBase 가 붙은 메카닉 프리팹")]
        [SerializeField] private GameObject mechanicPrefab;
        [Tooltip("스테이지 RT 해상도. 0이면 기본값 (240x150)")]
        [SerializeField] private Vector2Int stageResolution;
        [Tooltip("화면 투사 머티리얼 (선택). 비우면 창의 기본 렌즈 머티리얼")]
        [SerializeField] private Material screenMaterial;
        [SerializeField] private MinigameWindowLayout layout;

        [Header("Start")]
        [Tooltip("시작 조건 (예: flag:met_kid;!item:rose)")]
        [SerializeField] private string startConditions;
        [Tooltip("시작할 수 없을 때 트리거가 여는 Dialogue DataID. -1이면 상호작용 자체가 숨겨짐")]
        [SerializeField] private int deniedDialogueId = -1;
        [SerializeField] private string startActions;
        [SerializeField] private MinigameRepeatPolicy repeatPolicy;

        [Header("End")]
        [Tooltip("위에서부터 검사해 처음 만족하는 결과로 끝남")]
        [SerializeField] private MinigameOutcome[] outcomes;
        [Tooltip("메카닉이 중단하거나 창이 밖에서 닫힐 때 실행")]
        [SerializeField] private string abortActions;

        public string Id => id;
        public string Title => title;
        public GameObject MechanicPrefab => mechanicPrefab;
        public Vector2Int StageResolution => stageResolution.x > 0 && stageResolution.y > 0 ? stageResolution : MinigameDefines.DefaultStageResolution;
        public Material ScreenMaterial => screenMaterial;
        public MinigameWindowLayout Layout => layout;
        public string StartConditions => startConditions;
        public int DeniedDialogueId => deniedDialogueId;
        public string StartActions => startActions;
        public MinigameRepeatPolicy RepeatPolicy => repeatPolicy;
        public MinigameOutcome[] Outcomes => outcomes;
        public string AbortActions => abortActions;

        /// <summary>SourceBounds 배치에서 메카닉이 판을 만들 수 있는 최소 스테이지 크기. 메카닉 정의가 재정의합니다.</summary>
        public virtual Vector2Int MinStageResolution => MinigameDefines.MinStageResolution;

        public string PlaysFlag => MinigameDefines.FlagPrefix + id + MinigameDefines.PlaysFlagSuffix;
        public string ClearedFlag => MinigameDefines.FlagPrefix + id + MinigameDefines.ClearedFlagSuffix;
        public string GetOutcomeFlag(string outcomeName) => MinigameDefines.FlagPrefix + id + "_" + outcomeName;
    }
}
