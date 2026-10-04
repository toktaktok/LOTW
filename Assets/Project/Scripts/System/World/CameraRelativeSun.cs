using UnityEngine;
using Unity.Cinemachine;
using Project.Scripts.Data;

namespace Project.Scripts.System.World
{
    /// <summary>
    /// Directional Light를 현재 카메라 방향 기준으로 회전시켜, 카메라를 정면으로 보는 벽에서
    /// 그림자가 항상 화면상 정확한 45도(가로 1px : 세로 1px) 계단으로 떨어지게 합니다.
    /// 카메라마다 보는 방향이 달라도 그림자 모양이 같고, 카메라 블렌드 중에는 함께 회전합니다.
    /// 플레이 중에만 동작하므로 씬에 저장된 라이트 회전은 편집 모드용입니다.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class CameraRelativeSun : MonoBehaviour
    {
        [Header("Shadow Shape")]
        [Tooltip("태양 고도(도). 낮을수록 바닥 그림자가 길어짐")]
        [SerializeField, Range(CameraDefines.SunMinPitch, CameraDefines.SunMaxPitch)] private float pitch = CameraDefines.SunDefaultPitch;
        [Tooltip("켜면 그림자가 화면 왼쪽 아래로, 끄면 오른쪽 아래로 떨어짐")]
        [SerializeField] private bool shadowsFallLeft = true;

        private void OnEnable()
        {
            CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
        }

        private void OnDisable()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
        }

        private void OnCameraUpdated(CinemachineBrain brain)
        {
            var outputCamera = brain.OutputCamera;
            if(outputCamera == null)
                return;

            Vector3 forward = outputCamera.transform.forward;
            float cameraYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            float cameraPitch = Mathf.Asin(Mathf.Clamp(-forward.y, -1f, 1f));

            // 카메라를 정면으로 보는 수직 벽 위 그림자 변위의 화면 가로 성분은 cos(pitch)·sin(φ),
            // 세로 성분은 sin(pitch)·cos(cameraPitch). 둘이 같아지는 상대 yaw φ를 구함.
            float sinRelative = Mathf.Tan(pitch * Mathf.Deg2Rad) * Mathf.Cos(cameraPitch);
            float relativeYaw = Mathf.Asin(Mathf.Clamp(sinRelative, -1f, 1f)) * Mathf.Rad2Deg;
            if(shadowsFallLeft)
                relativeYaw = -relativeYaw;

            transform.rotation = Quaternion.Euler(pitch, cameraYaw + relativeYaw, 0f);
        }
    }
}
