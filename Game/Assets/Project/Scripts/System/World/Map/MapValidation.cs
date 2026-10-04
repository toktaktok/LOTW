using System.Collections.Generic;
using Project.Scripts.Data.Table;

namespace Project.Scripts.System.World.Map
{
    /// <summary>
    /// 저장 전/로드 후/EditMode 테스트에서 맵 무결성을 검사합니다.
    /// JsonUtility의 조용한 기본값 채움과 2-이웃 초과 손상을 잡아냅니다.
    /// </summary>
    public static class MapValidation
    {
        public static List<string> Validate(MapData data)
        {
            List<string> warnings = new List<string>();
            if(data == null)
            {
                warnings.Add("MapData is null.");
                return warnings;
            }

            HashSet<int> nodeIds = new HashSet<int>();
            if(data.nodes != null)
            {
                foreach(NodeEntry n in data.nodes)
                    if(!nodeIds.Add(n.id))
                        warnings.Add($"Duplicate node id {n.id}.");
            }
            if(nodeIds.Count == 0)
                warnings.Add("Map has no nodes.");

            Dictionary<int, int> degree = new Dictionary<int, int>();
            if(data.edges != null)
            {
                HashSet<long> seen = new HashSet<long>();
                foreach(EdgeEntry e in data.edges)
                {
                    if(e.a == e.b)
                    {
                        warnings.Add($"Self edge on node {e.a}.");
                        continue;
                    }
                    if(!nodeIds.Contains(e.a) || !nodeIds.Contains(e.b))
                    {
                        warnings.Add($"Edge {e.a}-{e.b} references a missing node.");
                        continue;
                    }
                    int lo = e.a < e.b ? e.a : e.b;
                    int hi = e.a < e.b ? e.b : e.a;
                    long key = ((long)lo << 32) | (uint)hi;
                    if(!seen.Add(key))
                    {
                        warnings.Add($"Duplicate edge {e.a}-{e.b}.");
                        continue;
                    }
                    degree.TryGetValue(e.a, out int da); degree[e.a] = da + 1;
                    degree.TryGetValue(e.b, out int db); degree[e.b] = db + 1;
                }
            }
            foreach(KeyValuePair<int, int> kv in degree)
                if(kv.Value > 2)
                    warnings.Add($"Node {kv.Key} has {kv.Value} neighbors (max 2).");

            HashSet<int> objIds = new HashSet<int>();
            if(data.placeables != null)
            {
                foreach(PlaceableEntry p in data.placeables)
                {
                    if(p.objectID != 0 && !objIds.Add(p.objectID))
                        warnings.Add($"Duplicate placeable objectID {p.objectID}.");
                    if(string.IsNullOrEmpty(p.prefabId))
                        warnings.Add($"Placeable objectID {p.objectID} has empty prefabId.");
                    if(p.linkedNodeId != -1 && !nodeIds.Contains(p.linkedNodeId))
                        warnings.Add($"Placeable objectID {p.objectID} links to missing node {p.linkedNodeId}.");
                }
            }

            return warnings;
        }
    }
}
