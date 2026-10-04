using System.Collections;
using Project.Scripts.Content.Controller;
using UnityEngine;
using Unity.Cinemachine;

using Project.Scripts.Data;

namespace Project.Scripts.Core.Managers
{
    public class CameraManager : Singleton<CameraManager>
    {
        #region Priorities
        
        [Header("Cinemachine")]
        [SerializeField] private CinemachineBrain brain;
        
        private CinemachineCamera _currentCamera;
        private Camera _renderCamera;
        private PlayerController _playerController;
        
        private CinemachineBlendDefinition _initialBlend; 
        private Coroutine _currentBlendRoutine;
        
        #endregion

        #region Methods
        
        private void Start()
        {
            _playerController = FindFirstObjectByType<PlayerController>();
            
            if(brain != null)
                _initialBlend = brain.DefaultBlend;
            else
                TryResolveBrain();
        }

        private bool TryResolveBrain()
        {
            if(brain != null)
                return true;

            if(Camera.main == null)
                return false;

            brain = Camera.main.GetComponent<CinemachineBrain>();
            if(brain == null)
                return false;

            _initialBlend = brain.DefaultBlend;
            return true;
        }

        public Camera GetCurrentCamera()
        {
            if(_renderCamera == null)
                _renderCamera = Camera.main;
            return _renderCamera;
        }
        public void SwitchCamera(CinemachineCamera targetCamera, float blendDuration = -1f)
        {
            if(targetCamera == null)
                return;

            if(_currentCamera == targetCamera)
                return;
    
            //Reset Priority of Last Camera
            if(_currentCamera != null)
                _currentCamera.Priority = CameraDefines.DefaultCameraPriority;
    
            //Smooth Camera
            if(blendDuration >= 0f)
            {
                if(_currentBlendRoutine != null)
                    StopCoroutine(_currentBlendRoutine);
                
                _currentBlendRoutine = StartCoroutine(BlendRoutine(blendDuration));
            }
            targetCamera.Priority = CameraDefines.FirstCameraPriority;
            _currentCamera = targetCamera;
        }
        private IEnumerator BlendRoutine(float duration)
        {
            if(!TryResolveBrain())
            {
                _currentBlendRoutine = null;
                yield break;
            }

            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, duration);
    
            yield return null;
            yield return new WaitForSeconds(duration + 0.1f);
            
            if(brain != null)
                brain.DefaultBlend = _initialBlend;
            _currentBlendRoutine = null;
        }
    
        public void SetInput(bool isEnabled)
        {
            if(_playerController == null)
                _playerController = FindFirstObjectByType<PlayerController>();

            if(_playerController != null)
                _playerController.enabled = isEnabled;
        }
        
        #endregion
    }
}