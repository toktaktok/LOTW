using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;
using Project.Scripts.Content.World;

namespace Project.Scripts.System.World.Map
{
    /// <summary>
    /// MapData/MapModel로부터 씬의 RailNode 그래프와 배치물을 재구성합니다.
    /// 노드는 프리팹 없이 AddComponent로 만들고, 배치물은 Resources/MapPrefabs/{prefabId} 에서 로드(없으면 건너뜀)합니다.
    /// 빌드된 모든 것은 [MapRoot] 아래에 모여 ClearBuilt로 멱등하게 재빌드됩니다.
    /// </summary>
    public class MapBuilder
    {
        private GameObject _root;
        private readonly Dictionary<int, RailNode> _nodeMap = new Dictionary<int, RailNode>();

        public GameObject Root => _root;
        public IReadOnlyDictionary<int, RailNode> NodeMap => _nodeMap;

        public void Build(MapModel model) => Build(model.ToData());

        public void Build(MapData data)
        {
            ClearBuilt();
            if(data == null)
                return;

            _root = new GameObject("[MapRoot]");

            // Pass 1: nodes
            if(data.nodes != null)
            {
                foreach(NodeEntry n in data.nodes)
                {
                    GameObject go = new GameObject($"RailNode_{n.id}");
                    go.transform.SetParent(_root.transform);
                    go.transform.position = n.position;

                    RailNode rn = go.AddComponent<RailNode>();
                    rn.nodeColor = n.nodeColor;
                    rn.radius = n.radius <= 0f ? MapDefines.DefaultNodeRadius : n.radius;
                    _nodeMap[n.id] = rn;
                }
            }

            // Pass 2: edges (RailNode.ConnectTo enforces max-2 + bidirectional)
            if(data.edges != null)
            {
                foreach(EdgeEntry e in data.edges)
                {
                    if(_nodeMap.TryGetValue(e.a, out RailNode a) && _nodeMap.TryGetValue(e.b, out RailNode b))
                        a.ConnectTo(b);
                }
            }

            // Pass 3: placeables (resolve linkedNodeId after all nodes exist)
            if(data.placeables != null)
            {
                foreach(PlaceableEntry p in data.placeables)
                    SpawnPlaceable(p);
            }
        }

        private void SpawnPlaceable(PlaceableEntry p)
        {
            if(string.IsNullOrEmpty(p.prefabId))
                return;

            GameObject prefab = Resources.Load<GameObject>($"MapPrefabs/{p.prefabId}");
            if(prefab == null)
            {
                Debug.LogWarning($"[MapBuilder] Missing prefab Resources/MapPrefabs/{p.prefabId}; skipping placeable {p.objectID}.");
                return;
            }

            GameObject go = Object.Instantiate(prefab, p.position, Quaternion.Euler(p.eulerAngles), _root.transform);

            WorldObject wo = go.GetComponent<WorldObject>();
            if(wo != null && p.objectID != 0)
                wo.SetObjectID(p.objectID);

            RailNode linked = null;
            if(p.linkedNodeId != -1)
                _nodeMap.TryGetValue(p.linkedNodeId, out linked);

            RailConnector connector = go.GetComponent<RailConnector>();
            if(connector != null && linked != null)
                connector.SetDestinationNode(linked);

            SceneEntrance entrance = go.GetComponent<SceneEntrance>();
            if(entrance != null)
            {
                if(linked != null)
                    entrance.SetStartNode(linked);
                if(!string.IsNullOrEmpty(p.entranceId))
                    entrance.SetEntranceId(p.entranceId);
            }
        }

        public void ClearBuilt()
        {
            if(_root != null)
            {
                if(Application.isPlaying)
                    Object.Destroy(_root);
                else
                    Object.DestroyImmediate(_root);
            }
            _root = null;
            _nodeMap.Clear();
        }

        public RailNode FirstNode()
        {
            foreach(KeyValuePair<int, RailNode> kv in _nodeMap)
                return kv.Value;
            return null;
        }
    }
}
