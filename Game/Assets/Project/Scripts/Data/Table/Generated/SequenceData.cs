// 자동 생성 파일. Table/Schema/Sequence.json 을 고치고 ConvertTable.bat 을 실행하세요.
using System;

namespace Project.Scripts.Data.Table
{
    [Serializable]
    public partial class SequenceData : TableRowData
    {
        /// <summary>dialogue, wait, fadeOut, fadeIn, camera, scene, actions (대소문자 무시)</summary>
        public string type;
        /// <summary>dialogue: Dialogue DataId, camera: 카메라 오브젝트 이름, scene: 씬 이름[:입구 ID], actions: 대화 액션 문법</summary>
        public string param;
        /// <summary>wait 초, fade/camera 시간 (0이면 기본값)</summary>
        public float duration;
        /// <summary>만족할 때만 실행, 아니면 건너뜀 (Dialogue 조건 문법)</summary>
        public string conditions;
        /// <summary>다음 스텝 DataId. -1이면 끝</summary>
        public int nextId;
    }
}
