using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Data;
using Project.Scripts.System.World;

namespace Project.Scripts.System.Minigame
{
    /// <summary>
    /// SourceBounds 배치 계산. 발생원 오브젝트의 3D 경계 상자를 월드 저해상도 RT 픽셀로 투영해
    /// 스테이지 크기와 화면 사각형을 구합니다. 스테이지 1픽셀 = 월드 RT 1픽셀이 되도록 같은 픽셀 격자에 놓습니다.
    /// 좌표: RT 픽셀과 화면 픽셀 모두 왼쪽 아래가 원점입니다.
    /// </summary>
    public static class MinigameProjection
    {
        private static readonly List<Renderer> Renderers = new();
        private static readonly Vector3[] Corners = new Vector3[8];

        /// <summary>
        /// 발생원(자식 포함) 렌더러들의 경계 상자 꼭짓점을 월드 카메라 RT 픽셀로 투영해 감싸는 사각형을 구합니다.
        /// 파티클, 선 렌더러는 뺍니다. 그릴 렌더러가 없거나 월드 렌더러가 준비되지 않았으면 false.
        /// </summary>
        public static bool TryProjectSource(GameObject source, LowResPixelRenderer view, out Rect pixels)
        {
            pixels = default;
            if(source == null || view == null || !view.IsReady)
                return false;

            Camera camera = view.Camera;
            Vector2 renderSize = view.RenderSize;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            bool found = false;

            source.GetComponentsInChildren(false, Renderers);
            foreach(Renderer renderer in Renderers)
            {
                if(!renderer.enabled || renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
                    continue;

                GetCorners(renderer.localBounds, renderer.transform.localToWorldMatrix);
                foreach(Vector3 corner in Corners)
                {
                    Vector3 viewport = camera.WorldToViewportPoint(corner);
                    Vector2 pixel = Vector2.Scale(viewport, renderSize);
                    min = Vector2.Min(min, pixel);
                    max = Vector2.Max(max, pixel);
                }
                found = true;
            }
            Renderers.Clear();

            if(!found)
                return false;

            pixels = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }

        /// <summary>투영 사각형을 덮는 정수 크기. min 이상, max 이하로 맞춥니다 (max가 0이면 위쪽 제한 없음).</summary>
        public static Vector2Int GetStageResolution(Rect pixels, Vector2Int min, Vector2Int max)
        {
            float tolerance = MinigameDefines.ProjectionSizeTolerance;
            int width = Mathf.Max(min.x, Mathf.CeilToInt(pixels.width - tolerance));
            int height = Mathf.Max(min.y, Mathf.CeilToInt(pixels.height - tolerance));
            if(max.x > 0)
                width = Mathf.Min(width, max.x);
            if(max.y > 0)
                height = Mathf.Min(height, max.y);
            return new Vector2Int(width, height);
        }

        /// <summary>center를 중심으로 하는 size 크기의 RT 정수 픽셀 사각형.</summary>
        public static RectInt GetStageRect(Vector2 center, Vector2Int size)
        {
            int xMin = Mathf.RoundToInt(center.x - size.x * 0.5f);
            int yMin = Mathf.RoundToInt(center.y - size.y * 0.5f);
            return new RectInt(xMin, yMin, size.x, size.y);
        }

        /// <summary>
        /// RT 픽셀 사각형이 화면에 그려지는 화면 픽셀 사각형. LowResPixelRenderer가 RT를
        /// 여백(margin)과 서브픽셀(subPixel)만큼 밀고 scale배로 그리는 것과 같은 변환입니다.
        /// </summary>
        public static Rect ToScreen(RectInt pixels, int margin, Vector2 subPixel, int scale)
        {
            float x = (pixels.xMin - margin - subPixel.x) * scale;
            float y = (pixels.yMin - margin - subPixel.y) * scale;
            return new Rect(x, y, pixels.width * scale, pixels.height * scale);
        }

        /// <summary>발생원 영역에 맞춘 스테이지 해상도. 투영할 수 없으면 false.</summary>
        public static bool TryGetStageResolution(MinigameDefinition definition, GameObject source, out Vector2Int resolution)
        {
            resolution = default;
            LowResPixelRenderer view = LowResPixelRenderer.Current;
            if(!TryProjectSource(source, view, out Rect pixels))
                return false;

            resolution = GetStageResolution(pixels, definition.MinStageResolution, view.RenderSize);
            return true;
        }

        /// <summary>
        /// 발생원 영역 중심에 size 크기 스테이지를 놓았을 때의 화면 픽셀 사각형.
        /// 매 프레임 불러 카메라가 움직여도 발생원을 따라가게 합니다. 투영할 수 없으면 false.
        /// </summary>
        public static bool TryGetScreenRect(GameObject source, Vector2Int size, out Rect screen)
        {
            screen = default;
            LowResPixelRenderer view = LowResPixelRenderer.Current;
            if(!TryProjectSource(source, view, out Rect pixels))
                return false;

            RectInt stage = GetStageRect(pixels.center, size);
            screen = ToScreen(stage, CameraDefines.LowResMarginPixels, view.SubPixel, view.Scale);
            return true;
        }

        private static void GetCorners(Bounds bounds, Matrix4x4 localToWorld)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            for(int i = 0; i < Corners.Length; i++)
            {
                Vector3 local = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                Corners[i] = localToWorld.MultiplyPoint3x4(local);
            }
        }
    }
}
