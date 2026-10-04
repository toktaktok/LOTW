using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project.Scripts.Content.World
{
    public class RailNode : MonoBehaviour
    {
        public List<RailNode> neighbors = new List<RailNode>();
        public Color nodeColor = Color.darkOrange;
        public float radius = 0.3f;

        private void OnValidate()
        {
            neighbors.RemoveAll(n => n == null);
            if(neighbors.Count > 2)
                neighbors.RemoveRange(2, neighbors.Count-2); 
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = nodeColor;
            Gizmos.DrawSphere(transform.position, radius);
            if(neighbors == null)
                return;

            Gizmos.color = Color.orangeRed;
            foreach(RailNode neighbor in neighbors)
            {
                if(neighbor != null)
                    Gizmos.DrawLine(transform.position, neighbor.transform.position);
            }
            
#if UNITY_EDITOR
            DrawGizmoLabel();
#endif
        }

#if UNITY_EDITOR
        private void DrawGizmoLabel()
        {
            string labelText = name;
            int underscoreIndex = name.LastIndexOf('_');

            if(underscoreIndex >= 0 && underscoreIndex < name.Length - 1)
            {
                labelText = name.Substring(underscoreIndex + 1);
            }

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.gray1;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 15;
            style.fontStyle = FontStyle.Bold;

            Vector3 labelPos = transform.position + Vector3.up * (radius + 0.5f);

            Handles.Label(labelPos, labelText, style);
        }
#endif
        
        public void ConnectTo(RailNode other)
        {
            if(other == null || this == other)
                return;

            if(neighbors.Count>=2 || other.neighbors.Count>=2)
            {
                Debug.LogWarning("Cannot connect: One of the nodes is full (Max 2).");
                return;
            }
            if(!neighbors.Contains(other))
                neighbors.Add(other);
            if(!other.neighbors.Contains(this))
                other.neighbors.Add(this);
        }

        public void Disconnect(RailNode other)
        {
            if(other == null)
                return;

            neighbors.Remove(other);
            other.neighbors.Remove(this);
        }

        /// <summary>exclude가 아닌 이웃을 반환합니다. 레일이 한 줄(이웃 최대 2)이라 "계속 진행" 방향이 됩니다.</summary>
        public RailNode GetOtherNeighbor(RailNode exclude)
        {
            foreach(RailNode neighbor in neighbors)
            {
                if(neighbor != null && neighbor != exclude)
                    return neighbor;
            }
            return null;
        }

        /// <summary>씬에서 position에 가장 가까운 RailNode. 없으면 null.</summary>
        public static RailNode FindNearest(Vector3 position)
        {
            RailNode best = null;
            float bestSqr = float.MaxValue;
            foreach(RailNode node in FindObjectsByType<RailNode>(FindObjectsSortMode.None))
            {
                float sqr = (node.transform.position - position).sqrMagnitude;
                if(sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = node;
                }
            }
            return best;
        }
    }
}