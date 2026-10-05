using System;

namespace Project.Scripts.Data.Table
{
    /// <summary>
    /// 다국어 텍스트 테이블 한 행.
    /// Excel: Table/Excel/Text_{분류}.xlsx  (DataId, Key, Ko, En). 분류: UI, Dialogue, Character, Item, World, Quest
    /// Key 는 '{분류 소문자}.{대상}[.{항목}]' (예: character.gumman.name). DataId 는 파일 안에서만 유일합니다.
    /// 다른 테이블이나 Inspector 문자열에서 '@Key' 로 참조하며 Localization.Resolve 가 변환합니다.
    /// 언어를 추가하려면 컬럼과 필드를 추가하고 Get()에 case 를 등록합니다.
    /// </summary>
    [Serializable]
    public class TextData : TableRowData
    {
        public string key;
        public string ko;
        public string en;

        /// <summary>언어 코드에 맞는 텍스트. 비어 있으면 기본 언어(ko)로 대체합니다.</summary>
        public string Get(string language)
        {
            string text = language switch
            {
                "en" => en,
                _ => ko,
            };
            return string.IsNullOrEmpty(text) ? ko : text;
        }
    }
}
