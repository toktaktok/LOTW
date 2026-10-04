using UnityEngine;
using Unity.Cinemachine;
using Project.Scripts.Core.Managers;

namespace Project.Scripts.System.World
{
    public class SceneCameraStarter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private CinemachineCamera sceneDefaultCamera;

        private void Start()
        {
            if(sceneDefaultCamera != null)
            {
                if(CameraManager.Instance != null)
                    CameraManager.Instance.SwitchCamera(sceneDefaultCamera, 0f);
            }
            else
                Debug.LogWarning($"[SceneCameraStarter] {gameObject.scene.name} 씬에 기본 카메라가 연결되지 않았습니다!");
        }
    }
}