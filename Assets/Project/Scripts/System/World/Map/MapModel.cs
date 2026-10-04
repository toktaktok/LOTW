using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Data.Table;

namespace Project.Scripts.System.World.Map
{
    /// <summary>
    /// 편집 중인 맵의 in-memory 단일 진실원본(source of truth).
    /// 모든 편집 연산은 여기서 먼저 일어나고, 씬의 RailNode는 이 모델로부터 재구성되는 뷰일 뿐입니다.
    /// nextId 하나가 노드 id와 WorldObject.ObjectID를 모두 발급해 유일성을 한 곳에서 보장합니다.
    /// </summary>
    public class MapModel
    {
        public string mapName = "untitled";

        public readonly List<NodeEntry> nodes = new List<NodeEntry>();
        public readonly List<EdgeEntry> edges = new List<EdgeEntry>();
        public readonly List<PlaceableEntry> placeables = new List<PlaceableEntry>();

        private int _nextId = 1;
        public int NextId => _nextId;
        public int AllocId() => _nextId++;

        #region Nodes

        public int AddNode(Vector3 position, Color color, float radius)
        {
            int id = AllocId();
            nodes.Add(new NodeEntry { id = id, position = position, nodeColor = color, radius = radius });
            return id;
        }

        public bool MoveNode(int id, Vector3 position)
        {
            for(int i = 0; i < nodes.Count; i++)
            {
                if(nodes[i].id != id)
                    continue;
                NodeEntry n = nodes[i];
                n.position = position;
                nodes[i] = n;
                return true;
            }
            return false;
        }

        public void DeleteNode(int id)
        {
            nodes.RemoveAll(n => n.id == id);
            edges.RemoveAll(e => e.a == id || e.b == id);
        }

        public int NeighborCount(int id)
        {
            int c = 0;
            foreach(EdgeEntry e in edges)
                if(e.a == id || e.b == id)
                    c++;
            return c;
        }

        #endregion

        #region Edges

        public bool HasEdge(int a, int b)
        {
            foreach(EdgeEntry e in edges)
                if((e.a == a && e.b == b) || (e.a == b && e.b == a))
                    return true;
            return false;
        }

        /// <summary>RailNode와 동일하게 노드당 최대 2개 이웃 제한을 사전 검사합니다.</summary>
        public bool AddEdge(int a, int b)
        {
            if(a == b || HasEdge(a, b))
                return false;
            if(NeighborCount(a) >= 2 || NeighborCount(b) >= 2)
            {
                Debug.LogWarning($"[MapModel] Cannot connect {a}-{b}: a node is full (max 2).");
                return false;
            }
            edges.Add(new EdgeEntry { a = a, b = b });
            return true;
        }

        public void RemoveEdge(int a, int b)
        {
            edges.RemoveAll(e => (e.a == a && e.b == b) || (e.a == b && e.b == a));
        }

        #endregion

        #region Placeables

        public int AddPlaceable(PlaceableEntry entry)
        {
            if(entry.objectID == 0)
                entry.objectID = AllocId();
            placeables.Add(entry);
            return entry.objectID;
        }

        public bool MovePlaceable(int objectID, Vector3 position)
        {
            for(int i = 0; i < placeables.Count; i++)
            {
                if(placeables[i].objectID != objectID)
                    continue;
                PlaceableEntry p = placeables[i];
                p.position = position;
                placeables[i] = p;
                return true;
            }
            return false;
        }

        public void DeletePlaceable(int objectID)
        {
            placeables.RemoveAll(p => p.objectID == objectID);
        }

        #endregion

        #region Conversion

        public MapData ToData()
        {
            return new MapData
            {
                dataId = 1,
                mapName = mapName,
                nextId = _nextId,
                nodes = nodes.ToArray(),
                edges = edges.ToArray(),
                placeables = placeables.ToArray(),
            };
        }

        /// <summary>다른 모델의 내용으로 이 인스턴스를 덮어씁니다 (readonly 참조 유지용).</summary>
        public void CopyFrom(MapModel other)
        {
            if(other == null)
                return;
            mapName = other.mapName;
            _nextId = other._nextId;
            nodes.Clear(); nodes.AddRange(other.nodes);
            edges.Clear(); edges.AddRange(other.edges);
            placeables.Clear(); placeables.AddRange(other.placeables);
        }

        public static MapModel FromData(MapData data)
        {
            MapModel m = new MapModel();
            if(data == null)
                return m;

            m.mapName = string.IsNullOrEmpty(data.mapName) ? "untitled" : data.mapName;
            m._nextId = Mathf.Max(1, data.nextId);
            if(data.nodes != null)
                m.nodes.AddRange(data.nodes);
            if(data.edges != null)
                m.edges.AddRange(data.edges);
            if(data.placeables != null)
                m.placeables.AddRange(data.placeables);
            return m;
        }

        #endregion
    }
}
