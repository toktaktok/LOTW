using System;
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
        [SerializeField] private CinemachineCamera defaultCamera;
        
        private CinemachineCamera _currentCamera;
        private PlayerController _playerController;
        
        private CinemachineBlendDefinition _initialBlend; 
        private Coroutine _currentBlendRoutine;
        
        #endregion

        #region Methods

        protected override void Awake()
        {
            base.Awake();
    
            if(brain == null && Camera.main != null)
                brain = Camera.main.GetComponent<CinemachineBrain>();

            if(brain != null)
                _initialBlend = brain.DefaultBlend;
        }

        private void Start()
        {
            _playerController = FindFirstObjectByType<PlayerController>();
    
            if(defaultCamera != null)
                SetCamera(defaultCamera, 0f);
        }

        public void SetCamera(CinemachineCamera targetCamera, float blendDuration = -1f)
        {
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
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, duration);
    
            yield return null;
            yield return new WaitForSeconds(duration + 0.1f);
            
            brain.DefaultBlend = _initialBlend;
            _currentBlendRoutine = null;
        }
    
        public void SetInput(bool isEnabled)
        {
            if(_playerController != null)
                _playerController.enabled = isEnabled;
        }
        
        #endregion
    }
}