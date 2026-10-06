using System;
using UnityEngine;

namespace Project.Scripts.System.Minigame
{
    /// <summary>
    /// 미니게임 전용 입력 (Minigame 액션 맵). 창이 다 열린 뒤부터 닫히기 전까지만 켜집니다.
    /// 포인터는 화면 좌표 -> 창의 화면 영역 -> 스테이지 월드 좌표로 바꿔 줍니다.
    /// </summary>
    public class MinigameInput : IDisposable
    {
        private readonly PlayerControls _controls = new();
        private RectTransform _screenRect;
        private Camera _stageCamera;

        public Vector2 Navigate => _controls.Minigame.Navigate.ReadValue<Vector2>();
        public bool SubmitPressed => _controls.Minigame.Submit.WasPressedThisFrame();
        public bool SubmitHeld => _controls.Minigame.Submit.IsPressed();
        public bool AltPressed => _controls.Minigame.Alt.WasPressedThisFrame();
        public bool AltHeld => _controls.Minigame.Alt.IsPressed();
        public bool ClickPressed => _controls.Minigame.Click.WasPressedThisFrame();
        public bool ClickHeld => _controls.Minigame.Click.IsPressed();
        /// <summary>나가기 (Backspace / 패드 B). Esc는 메타 연출 전용이라 쓰지 않음.</summary>
        public bool CancelPressed => _controls.Minigame.Cancel.WasPressedThisFrame();
        /// <summary>포인터 화면 좌표 (픽셀).</summary>
        public Vector2 PointerScreen => _controls.Minigame.Point.ReadValue<Vector2>();

        /// <summary>창의 화면 영역(RawImage)과 스테이지 카메라를 연결합니다.</summary>
        public void Bind(RectTransform screenRect, Camera stageCamera)
        {
            _screenRect = screenRect;
            _stageCamera = stageCamera;
        }

        public void Enable() => _controls.Minigame.Enable();
        public void Disable() => _controls.Minigame.Disable();
        public void Dispose() => _controls.Dispose();

        /// <summary>포인터가 화면 영역 안에 있으면 스테이지 월드 좌표(카메라 평면 기준 x, y)를 돌려줍니다.</summary>
        public bool TryGetPointerWorld(out Vector3 world)
        {
            return TryScreenToWorld(PointerScreen, out world);
        }

        /// <summary>화면 좌표(픽셀)를 스테이지 월드 좌표로 바꿉니다. 화면 영역 밖이면 false.</summary>
        public bool TryScreenToWorld(Vector2 screen, out Vector3 world)
        {
            world = default;
            if(_screenRect == null || _stageCamera == null)
                return false;

            // 창은 Screen Space Overlay 캔버스라 카메라 없이 변환
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(_screenRect, screen, null, out Vector2 local))
                return false;

            Rect rect = _screenRect.rect;
            Vector2 uv = new Vector2((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height);
            if(uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f)
                return false;

            world = _stageCamera.ViewportToWorldPoint(new Vector3(uv.x, uv.y, -_stageCamera.transform.localPosition.z));
            return true;
        }
    }
}
