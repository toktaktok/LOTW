using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Project.Scripts.Content.Controller;
using Project.Scripts.System.World;

namespace Project.Scripts.Content.World
{
    /// <summary>
    /// 메인 카메라 렌더 직전에 TiltShiftVolume 값과 플레이어 화면 높이를 TiltShift.shader 전역 값으로 넘깁니다.
    /// 씬 배치 없이 자동으로 동작하며, 플레이어가 없으면 화면 중앙을 초점으로 씁니다.
    /// 플레이 중인 메인 카메라에만 적용되고, Scene 뷰와 편집 모드에서는 흐림 반경을 0으로 둡니다.
    /// </summary>
    public static class TiltShiftDriver
    {
        private static readonly int FocusCenterId = Shader.PropertyToID("_TiltShiftFocusCenter");
        private static readonly int FocusWidthId = Shader.PropertyToID("_TiltShiftFocusWidth");
        private static readonly int FalloffId = Shader.PropertyToID("_TiltShiftFalloff");
        private static readonly int MaxRadiusId = Shader.PropertyToID("_TiltShiftMaxRadius");
        private static readonly int SampleCountId = Shader.PropertyToID("_TiltShiftSampleCount");
        private static readonly int FocusSaturationId = Shader.PropertyToID("_TiltShiftFocusSaturation");
        private static readonly int CameraPosId = Shader.PropertyToID("_TiltShiftCameraPos");

        private const float ScreenCenter = 0.5f;
        // 어떤 카메라와도 겹치지 않는 위치. 플레이 종료 후 모든 카메라에서 효과가 꺼지게 함
        private const float NoCameraPosition = 1e9f;

        private static PlayerController _player;
        private static bool _searched;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
            _searched = false;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => _searched = false;

        // 에디터에서는 플레이 종료 시 호출됨. 전역 값이 남아 편집 화면에 흐림이 보이지 않게 끔
        private static void OnQuitting()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Application.quitting -= OnQuitting;
            Shader.SetGlobalFloat(MaxRadiusId, 0f);
            Shader.SetGlobalVector(CameraPosId, Vector3.one * NoCameraPosition);
            _player = null;
        }

        private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            // 전역 값은 카메라별로 바꿔도 마지막 값이 모든 카메라에 쓰이므로, 다른 카메라(Scene 뷰 등)는
            // 셰이더가 _TiltShiftCameraPos와 자기 카메라 위치를 비교해 걸러냄
            if(camera.cameraType != CameraType.Game || camera != Camera.main)
                return;

            Shader.SetGlobalVector(CameraPosId, camera.transform.position);

            // 씬마다 한 번만 찾음 (플레이어가 없는 씬에서 매 프레임 검색하지 않도록)
            if(_player == null && !_searched)
            {
                _player = Object.FindFirstObjectByType<PlayerController>();
                _searched = true;
            }

            var settings = VolumeManager.instance.stack.GetComponent<TiltShiftVolume>();

            float center = ScreenCenter;
            if(_player != null && _player.CurrentCharacter != null)
                center = camera.WorldToViewportPoint(_player.CurrentCharacter.Position).y;

            Shader.SetGlobalFloat(FocusCenterId, center + settings.focusOffset.value);
            Shader.SetGlobalFloat(FocusWidthId, settings.focusWidth.value);
            Shader.SetGlobalFloat(FalloffId, settings.falloff.value);
            Shader.SetGlobalFloat(MaxRadiusId, settings.maxRadius.value);
            Shader.SetGlobalFloat(SampleCountId, settings.sampleCount.value);
            Shader.SetGlobalFloat(FocusSaturationId, settings.focusSaturation.value);
        }
    }
}
