using System;
using UnityEngine;

namespace Project.Scripts.Data.Table
{
    /// <summary>
    /// 맵 한 장의 직렬화 형태. JsonUtility 친화적이어야 하므로
    /// 참조/Dictionary/다형성 없이 정수 인접 리스트(edges)로 레일 위상을 표현합니다.
    /// 디스크(유저 맵): persistentDataPath/Maps/map_{name}.json = 단일 MapData 객체.
    /// 기본 맵: Resources/Table/Map.json = 1개짜리 배열 [ {..} ] (DataManager 파이프라인 호환).
    /// </summary>
    [Serializable]
    public class MapData : TableRowData
    {
        public string mapName;
        public int nextId = 1;
        public NodeEntry[] nodes;
        public EdgeEntry[] edges;
        public PlaceableEntry[] placeables;
    }

    [Serializable]
    public struct NodeEntry
    {
        public int id;
        public Vector3 position;
        public Color nodeColor;
        public float radius;
    }

    [Serializable]
    public struct EdgeEntry
    {
        public int a;
        public int b;
    }

    [Serializable]
    public struct PlaceableEntry
    {
        public string prefabId;
        public Vector3 position;
        public Vector3 eulerAngles;
        public int objectID;
        public int linkedNodeId;   // -1 = 연결 없음 (0이 아님: JsonUtility 누락-필드 0채움 모호성 회피)
        public string entranceId;
        public string targetScene;
        public string targetEntranceId;
    }
}
