using System;
using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Data;
using Project.Scripts.Framework;
using MapData = Project.Scripts.Data.Table.MapData;

namespace Project.Scripts.Core.Managers
{
    /// <summary>
    /// 게임 시작 시 Resources/Table/*.json 을 읽어 테이블 데이터를 캐싱합니다.
    /// 내부 저장은 Dictionary&lt;dataId, row&gt; 형태이며 항상 dataId 를 통해 접근합니다.
    ///
    /// ──────────────────────────────────────────────────────────
    /// 새 테이블을 추가하는 절차:
    ///   1. Table/Schema/Xxx.json 에 rowClass 와 컬럼 자료형/규칙 작성
    ///   2. Table/Excel/ 에 Xxx.xlsx 작성 (B2 표, 헤더 아래 자료형 행) → ConvertTable.bat 실행
    ///      → JSON, 필드 클래스(Data/Table/Generated/XxxData.cs), 로드 목록(Generated/DataManager.Tables.cs)이 생성됨
    ///
    /// 테이블 파일 명명 규칙:
    ///   접미사 'Table' 없이 사용. 예) Dialogue.xlsx, Item.xlsx
    /// ──────────────────────────────────────────────────────────
    ///
    /// 사용 예:
    ///   DialogueData row  = DataManager.Instance.GetRow&lt;DialogueData&gt;(101);
    ///   var          rows = DataManager.Instance.GetRows&lt;DialogueData&gt;();
    /// </summary>
    public partial class DataManager : Singleton<DataManager>
    {
        #region State

        /// <summary>모든 테이블 로드가 완료됐는지 여부</summary>
        public bool IsLoaded { get; private set; }

        // 타입 → Dictionary<dataId, row> 로 캐싱. 항상 dataId 를 키로 접근.
        private readonly Dictionary<Type, object> _cache = new();

        #endregion

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();
            // 테이블 JSON 은 작아서 동기 로드한다. 비동기면 첫 프레임에 읽는 쪽이 null 을 받는다.
            LoadAllTables();
        }

        #endregion

        #region Public API

        /// <summary>
        /// dataId 로 단일 행을 조회합니다.
        /// 없으면 null 반환.
        /// </summary>
        public T GetRow<T>(int dataId) where T : TableRowData
        {
            if(_cache.TryGetValue(typeof(T), out object tableObj))
            {
                var dict = (Dictionary<int, T>)tableObj;
                return dict.TryGetValue(dataId, out T row) ? row : null;
            }
            return null;
        }

        /// <summary>
        /// 특정 테이블의 모든 행을 반환합니다.
        /// 로드되지 않은 타입이면 빈 컬렉션 반환.
        /// </summary>
        public IReadOnlyCollection<T> GetRows<T>() where T : TableRowData
        {
            if(_cache.TryGetValue(typeof(T), out object tableObj))
                return ((Dictionary<int, T>)tableObj).Values;
            return Array.Empty<T>();
        }

        #endregion

        #region Table Registration

        /// <summary>
        /// 파일명 규칙: Resources/Table/{name}.json (접미사 'Table' 없음)
        /// 스키마 테이블 목록은 변환기가 생성합니다 (Generated/DataManager.Tables.cs).
        /// </summary>
        private void LoadAllTables()
        {
            LoadSchemaTables();
            // Map 은 맵 에디터가 쓰는 JSON 이라 스키마가 없음
            LoadTable<MapData>("Map");
            // Text_* 테이블은 Key 로만 찾으므로 Localization 이 직접 읽음

            IsLoaded = true;
        }

        #endregion

        #region Internal Loading

        private void LoadTable<T>(string tableName) where T : TableRowData
        {
            if(Resources.Load<TextAsset>($"Table/{tableName}") is not TextAsset textAsset)
            {
                Debug.LogError(
                    $"[DataManager] 파일 없음: Resources/Table/{tableName}.json\n" +
                    "ConvertTable.bat 를 실행했는지 확인하세요.");
                return;
            }

            List<T> rows = JsonArrayHelper.FromJson<T>(textAsset.text);
            if(rows == null || rows.Count == 0)
            {
                Debug.LogWarning($"[DataManager] {tableName}: 파싱 결과가 비어 있습니다.");
                return;
            }

            var dict = new Dictionary<int, T>(rows.Count);
            foreach(T row in rows)
            {
                if(dict.ContainsKey(row.dataId))
                    Debug.LogWarning($"[DataManager] {tableName}: 중복 dataId = {row.dataId} (나중 값 사용)");
                dict[row.dataId] = row;
            }

            _cache[typeof(T)] = dict;
        }

        #endregion
    }
}
