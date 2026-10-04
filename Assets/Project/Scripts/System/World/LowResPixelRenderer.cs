using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using Project.Scripts.Data;

namespace Project.Scripts.System.World
{
    /// <summary>
    /// 카메라를 저해상도 RenderTexture로 렌더링하고 화면에 정수 배율(Point)로 확대해 표시합니다.
    /// 스프라이트 텍셀 1개 = RT 픽셀 1개가 되도록 orthographic size를 정수 배율 단계로 스냅하고,
    /// 카메라 위치를 텍셀 격자에 맞춰 이동 시 반짝임을 막습니다.
    /// 스냅으로 잘린 나머지는 RT 여백 안에서 표시 UV를 밀어 보정해, 스크롤은 부드럽게 유지합니다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class LowResPixelRenderer : MonoBehaviour
    {
        private Camera _camera;
        private RenderTexture _renderTexture;
        private RawImage _view;
        private int _scale;

        private void OnEnable()
        {
            _camera = GetComponent<Camera>();

            var viewObject = new GameObject("[LowResView]", typeof(Canvas), typeof(RawImage));
            var canvas = viewObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CameraDefines.LowResViewSortingOrder;

            _view = viewObject.GetComponent<RawImage>();
            _view.raycastTarget = false;
            var rect = _view.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
        }

        private void OnDisable()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);

            _camera.targetTexture = null;
            if(_view != null)
                Destroy(_view.gameObject);
            if(_renderTexture != null)
                _renderTexture.Release();
            _renderTexture = null;
        }

        private void OnCameraUpdated(CinemachineBrain brain)
        {
            if(brain.OutputCamera != _camera)
                return;

            // 가상 카메라가 정한 줌에 가장 가까운 정수 배율을 고른 뒤, 그 배율에서 텍셀이 1:1이 되도록 size를 재설정
            float pixelsPerUnit = CameraDefines.PixelsPerUnit;
            int margin = CameraDefines.LowResMarginPixels;
            _scale = Mathf.Max(1, Mathf.RoundToInt(Screen.height / (2f * _camera.orthographicSize * pixelsPerUnit)));
            int height = Mathf.CeilToInt((float)Screen.height / _scale) + 2 * margin;
            int width = Mathf.CeilToInt((float)Screen.width / _scale) + 2 * margin;
            EnsureRenderTexture(width, height);

            _camera.orthographicSize = height / (2f * pixelsPerUnit);

            // 카메라 평면 기준으로 위치를 텍셀 격자에 스냅
            float texel = 1f / pixelsPerUnit;
            Vector3 local = Quaternion.Inverse(transform.rotation) * transform.position;
            Vector2 snapped = new Vector2(Mathf.Round(local.x / texel) * texel, Mathf.Round(local.y / texel) * texel);
            Vector2 subPixel = (new Vector2(local.x, local.y) - snapped) / texel;
            local.x = snapped.x;
            local.y = snapped.y;
            transform.position = transform.rotation * local;

            // 스냅으로 잘린 나머지(-0.5~0.5 RT 픽셀)만큼 표시 영역을 밀어 스크롤을 부드럽게 유지.
            // RT를 정확히 _scale 배로 표시하고, 여백과 화면을 넘는 부분은 잘라냄
            _view.uvRect = new Rect((margin + subPixel.x) / width, (margin + subPixel.y) / height,
                Screen.width / (float)(width * _scale), Screen.height / (float)(height * _scale));
        }

        private void EnsureRenderTexture(int width, int height)
        {
            if(_renderTexture == null || _renderTexture.width != width || _renderTexture.height != height)
            {
                if(_renderTexture != null)
                    _renderTexture.Release();

                _renderTexture = new RenderTexture(width, height, 24) { filterMode = FilterMode.Point, name = "LowResPixelRT" };
                _camera.targetTexture = _renderTexture;
                _view.texture = _renderTexture;
            }
        }
    }
}
