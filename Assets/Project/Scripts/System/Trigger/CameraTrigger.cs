using UnityEngine;
using Unity.Cinemachine;
using Project.Scripts.Core.Managers;

namespace Project.Scripts.System.Trigger
{
    [RequireComponent(typeof(BoxCollider))]
    public class CameraTrigger : MonoBehaviour
    {
        [Header("Enter Settings")]
        [Tooltip("구역 진입 시 카메라")]
        [SerializeField] private CinemachineCamera targetCamera;
        [Tooltip("전환 소요 시간 (초)")]
        [SerializeField] private float blendDuration = 1.5f;

        [Header("Exit Settings")]
        [Tooltip("구역 이탈 시 돌아갈 카메라 (비워두면 안 돌아감)")]
        [SerializeField] private CinemachineCamera exitCamera;
        [Tooltip("전환 소요 시간 (초)")]
        [SerializeField] private float exitBlendDuration = 1.0f;
        
        #region Methods
        
        private void Start()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if(other.CompareTag("Player") && targetCamera != null)
                CameraManager.Instance.SwitchCamera(targetCamera, blendDuration);
        }
        private void OnTriggerExit(Collider other)
        {
            if(other.CompareTag("Player") && exitCamera != null)
                CameraManager.Instance.SwitchCamera(exitCamera, exitBlendDuration);
        }
        
        private void OnDrawGizmos()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if(col == null)
                return;

            Matrix4x4 origMat = Gizmos.matrix;
            Gizmos.matrix = col.transform.localToWorldMatrix;
            Gizmos.color = new Color(0f, 0.5450981f, 0.5450981f, 0.2f);
            Gizmos.DrawCube(col.center, col.size);
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(col.center, col.size);
            Gizmos.matrix = origMat;
        }
        
        #endregion
    }
}