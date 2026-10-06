// 자동 생성 파일. Table/Schema/Text.json 을 고치고 ConvertTable.bat 을 실행하세요.
using System;

namespace Project.Scripts.Data.Table
{
    [Serializable]
    public partial class TextData : TableRowData
    {
        /// <summary>{분류}.{대상}[.{항목}], 소문자/숫자/밑줄. 다른 테이블에서 '@Key'로 참조</summary>
        public string key;
        /// <summary>한국어 (기본 언어)</summary>
        public string ko;
        /// <summary>영어. 비우면 Ko 사용</summary>
        public string en;
    }
}
