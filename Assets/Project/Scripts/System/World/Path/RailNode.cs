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
    }
}