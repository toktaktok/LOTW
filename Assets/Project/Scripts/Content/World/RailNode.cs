using UnityEngine;
using System.Collections.Generic;

namespace Project.Scripts.Content.World
{
    public class RailNode : MonoBehaviour
    {
        public List<RailNode> neighbors = new List<RailNode>();
        public Color nodeColor = Color.yellow;
        public float radius = 0.3f;

        private void OnDrawGizmos()
        {
            Gizmos.color = nodeColor;
            Gizmos.DrawSphere(transform.position, radius);

            if(neighbors == null)
                return;

            Gizmos.color = Color.cyan;
            foreach(var neighbor in neighbors)
            {
                if(neighbor != null)
                {
                    Gizmos.DrawLine(transform.position, neighbor.transform.position);
                }
            }
        }

        public void ConnectTo(RailNode other)
        {
            if(other == null || this == other)
                return;

            if(!neighbors.Contains(other))
                neighbors.Add(other);
            if(!other.neighbors.Contains(this))
                other.neighbors.Add(this);
        }
    }
}