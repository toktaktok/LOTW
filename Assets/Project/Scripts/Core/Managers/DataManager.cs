using System;
using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Data;
using DialogueData = Project.Scripts.Data.Table.DialogueData;

namespace Project.Scripts.Core.Managers
{
    /// <summary>
    /// 게임 시작 시 Resources/Table/*.json 을 읽어 테이블 데이터를 캐싱합니다.
    /// 내부 저장은 Dictionary&lt;dataId, row&gt; 형태이며 항상 dataId 를 통해 접근합니다.
    ///
    /// ──────────────────────────────────────────────────────────
    /// 새 테이블을 추가하는 절차:
    ///   1. TableRowData 를 상속받는 데이터 클래스 생성 (Data/Table/XxxData.cs)
    ///   2. LoadAllTables() 에 LoadTable&lt;XxxData&gt;("Xxx") 한 줄 추가
    ///   3. Table/Excel/ 에 Xxx.xml 작성 (표 정의 필수) → ConvertTable.bat 실행
    ///
    /// 테이블 파일 명명 규칙:
    ///   XxxTable.xml 에서 'Table' 을 뺀 이름 사용. 예) Dialogue.xml, Item.xml
    /// ──────────────────────────────────────────────────────────
    ///
    /// 사용 예:
    ///   DialogueData row  = DataManager.Instance.GetRow&lt;DialogueData&gt;(101);
    ///   var          rows = DataManager.Instance.GetRows&lt;DialogueData&gt;();
    /// </summary>
    public class DataManager : Singleton<DataManager>
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
            LoadAllTables().Forget();
        }

        #endregion

        #region Public API

        /// <summary>
        /// dataId 로 단일 행을 조회합니다.
        /// 없으면 null 반환.
        /// </summary>
        public T GetRow<T>(int dataId) where T : TableRowData
        {
            if (_cache.TryGetValue(typeof(T), out object tableObj))
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
            if (_cache.TryGetValue(typeof(T), out object tableObj))
                return ((Dictionary<int, T>)tableObj).Values;
            return Array.Empty<T>();
        }

        #endregion

        #region Table Registration

        /// <summary>
        /// 로드할 테이블을 여기에 등록합니다.
        /// 파일명 규칙: Resources/Table/{name}.json (접미사 'Table' 없음)
        /// </summary>
        private async Awaitable LoadAllTables()
        {
            await LoadTable<DialogueData>("Dialogue");

            // ── 새 테이블 추가 시 아래에 등록 ──────────────────────
            // await LoadTable<ItemData>("Item");
            // await LoadTable<QuestData>("Quest");
            // ────────────────────────────────────────────────────────

            IsLoaded = true;
            Debug.Log("[DataManager] 모든 테이블 로드 완료");
        }

        #endregion

        #region Internal Loading

        private async Awaitable LoadTable<T>(string tableName) where T : TableRowData
        {
            ResourceRequest req = Resources.LoadAsync<TextAsset>($"Table/{tableName}");
            while (!req.isDone)
                await Awaitable.NextFrameAsync();

            if (req.asset is not TextAsset textAsset)
            {
                Debug.LogError(
                    $"[DataManager] 파일 없음: Resources/Table/{tableName}.json\n" +
                    "ConvertTable.bat 를 실행했는지 확인하세요.");
                return;
            }

            List<T> rows = JsonArrayHelper.FromJson<T>(textAsset.text);
            if (rows == null || rows.Count == 0)
            {
                Debug.LogWarning($"[DataManager] {tableName}: 파싱 결과가 비어 있습니다.");
                return;
            }

            var dict = new Dictionary<int, T>(rows.Count);
            foreach (T row in rows)
            {
                if (dict.ContainsKey(row.dataId))
                    Debug.LogWarning($"[DataManager] {tableName}: 중복 dataId = {row.dataId} (나중 값 사용)");
                dict[row.dataId] = row;
            }

            _cache[typeof(T)] = dict;
            Debug.Log($"[DataManager] {tableName}: {dict.Count}개 행 로드");
        }

        #endregion
    }
}
