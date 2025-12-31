using System;
using UnityEngine;
using Project.Scripts.Content.Controller;

namespace Project.Scripts.System.Trigger
{
    [RequireComponent(typeof(BoxCollider))]
    public class AxisChangeTrigger : MonoBehaviour
    {
        [SerializeField] private Vector3 targetAxis = Vector3.right;
        
        [Header("Debug Settings")]
        [SerializeField] private Color triggerColor = new Color(1, 1, 0, 0.5f);
        [SerializeField] private bool showArrow = true;

        public Vector3 TargetAxis { get => targetAxis; set => targetAxis = value; }
        
        #region Methods

        private void Start()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.CompareTag("Player"))
            {
                PlayerController controller = FindFirstObjectByType<PlayerController>();
                // if(controller != null)
                //     controller.SetMoveAxis(targetAxis);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = triggerColor;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            
            BoxCollider box = GetComponent<BoxCollider>();
            if (box != null)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = new Color(triggerColor.r, triggerColor.g, triggerColor.b, 1f);
                Gizmos.DrawWireCube(box.center, box.size);
            }

            if (showArrow)
            {
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = Color.red;
                Vector3 center = transform.position + box.center;
                Vector3 direction = targetAxis.normalized * 2f;
                
                Gizmos.DrawLine(center, center + direction);
                Gizmos.DrawSphere(center + direction, 0.2f);
            }
        }
        
        #endregion
    }
}