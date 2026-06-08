using System;
using UnityEngine;

namespace Project.Scripts.Data
{
    /// <summary>
    /// 모든 테이블 데이터의 기반 클래스.
    /// DataManager가 테이블별로 데이터를 저장·관리할 때 이 타입으로 일괄 접근합니다.
    ///
    /// 새 테이블을 추가할 때:
    ///   1. 이 클래스를 상속받는 [Serializable] 클래스를 Data/Table/ 에 생성
    ///   2. DataManager.LoadAllTables() 에 LoadTable 호출 등록
    ///   3. Table/Excel/ 에 XML 파일 추가 후 ConvertTable.bat 실행
    /// </summary>
    [Serializable]
    public abstract class TableRowData
    {
        /// <summary>테이블 내 고유 식별자. Excel DataID 컬럼과 1:1 매핑.</summary>
        public int dataId;
    }
}
