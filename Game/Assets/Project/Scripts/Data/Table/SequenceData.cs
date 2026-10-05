using System;

namespace Project.Scripts.Data.Table
{
    /// <summary>
    /// 연출 시퀀스의 스텝 한 행. 시작 스텝 dataId 부터 nextId(-1 이면 끝)를 따라 SequencePlayer 가 순서대로 실행합니다.
    /// Excel: Table/Excel/Sequence.xlsx  (DataId, Type, Param, Duration, Conditions, NextId)
    ///   dialogue  param = Dialogue dataId. 대화가 닫힐 때까지 대기
    ///   wait      duration 초 (게임 시간, 일시정지 중 멈춤)
    ///   fadeout   화면을 가림 / fadein 화면을 엶. duration 0 이면 StoryDefines.SequenceFadeDuration
    ///   camera    param = 씬의 CinemachineCamera 오브젝트 이름. duration = 블렌드 시간(0 이면 기본)
    ///   scene     param = 씬 이름[:입구 ID]. 전환이 끝날 때까지 대기
    ///   actions   param = 대화 액션 문법 (setFlag:timeSlot=1;startQuest:1;bgm:...)
    /// conditions 가 있으면 만족할 때만 실행하고, 아니면 건너뛰고 nextId 로 갑니다.
    /// </summary>
    [Serializable]
    public class SequenceData : TableRowData
    {
        public string type;
        public string param;
        public float duration;
        public string conditions;
        public int nextId;

        public bool TryGetStepType(out SequenceStepType stepType)
        {
            return TryParseType(type, out stepType);
        }

        /// <summary>대소문자 무시 ("fadeOut", "fadeout" 모두 허용).</summary>
        public static bool TryParseType(string value, out SequenceStepType stepType)
        {
            stepType = default;
            return !string.IsNullOrEmpty(value) && Enum.TryParse(value, true, out stepType) && Enum.IsDefined(typeof(SequenceStepType), stepType);
        }
    }
}
