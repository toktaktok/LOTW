using UnityEngine;
using Project.Scripts.Core.Managers;
using Unity.Cinemachine;
using UnityEngine.Serialization;

namespace Project.Scripts.System.World
{
    public class BillboardHandler : MonoBehaviour
    {
        [Header("Ghost Settings")]
        [SerializeField] private bool detachFromParent = true;
        [SerializeField] private Transform followTarget;

        [Header("Billboard Settings")]
        [SerializeField] private bool lockYAxis = true;
        [FormerlySerializedAs("positionOffset")] [SerializeField] private Vector3 additionalOffset = Vector3.zero;
        
        private GameObject _targetGameObject;
        private Vector3 _autoOffset;
        private Renderer[] _renderers;
        private bool _isVisible = true;
        
        private void Start()
        {
            if(followTarget == null)
                followTarget = transform.parent;

            if(followTarget != null)
            {
                _targetGameObject = followTarget.gameObject;
                _autoOffset = transform.position - followTarget.position;
            }

            if(detachFromParent && transform.parent != null)
            {
                transform.SetParent(null);
            }
            
            _renderers = GetComponentsInChildren<Renderer>(true);
            CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
        }

        private void OnDestroy()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
        }

        private void OnCameraUpdated(CinemachineBrain brain)
        {
            if(followTarget == null)
                return;
            
            if(!CameraManager.HasInstance)
                return;

            Camera targetCamera = CameraManager.Instance.GetCurrentCamera();
            if(targetCamera == null)
                return;

            transform.position = followTarget.position + _autoOffset + additionalOffset;
            if(lockYAxis)
            {
                float cameraY = targetCamera.transform.rotation.eulerAngles.y;
                transform.rotation = Quaternion.Euler(0f, cameraY, 0f);
            }
            else
            {
                transform.rotation = targetCamera.transform.rotation;
            }
        }

        private void Update()
        {
            if(followTarget == null)
            {
                Destroy(gameObject);
                return;
            }

            if(_targetGameObject != null)
            {
                bool isParentActive = _targetGameObject.activeInHierarchy;
                if(_isVisible != isParentActive)
                    SetRenderersEnabled(isParentActive);
            }
        }

        private void SetRenderersEnabled(bool value)
        {
            _isVisible = value;
            foreach(Renderer childRenderer in _renderers)
                childRenderer.enabled = value;
        }
    }
}