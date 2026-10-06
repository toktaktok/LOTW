using UnityEngine;

namespace Project.Scripts.Content.Minigame.GateDrop
{
    /// <summary>
    /// 실행 중에 임시 픽셀 그림을 그리는 작은 캔버스. 왼쪽 아래가 (0, 0)이고, 범위 밖 픽셀은 무시합니다.
    /// ToTexture는 Point 필터 텍스처를 만듭니다. 만든 텍스처의 해제는 호출한 쪽이 맡습니다.
    /// </summary>
    public class PixelCanvas
    {
        private readonly Color32[] _pixels;

        public int Width { get; }
        public int Height { get; }

        public PixelCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            _pixels = new Color32[width * height];
        }

        public void Set(int x, int y, Color color)
        {
            if(x < 0 || y < 0 || x >= Width || y >= Height)
                return;
            _pixels[y * Width + x] = color;
        }

        public Color Get(int x, int y)
        {
            if(x < 0 || y < 0 || x >= Width || y >= Height)
                return Color.clear;
            return _pixels[y * Width + x];
        }

        public void Fill(Color color) => FillRect(0, 0, Width, Height, color);

        public void FillRect(int x, int y, int width, int height, Color color)
        {
            for(int iy = y; iy < y + height; iy++)
            {
                for(int ix = x; ix < x + width; ix++)
                    Set(ix, iy, color);
            }
        }

        public void HLine(int x, int y, int length, Color color) => FillRect(x, y, length, 1, color);

        public void VLine(int x, int y, int length, Color color) => FillRect(x, y, 1, length, color);

        /// <summary>테두리만 그리는 사각형. 모서리 1픽셀을 비우면 둥근 느낌이 남.</summary>
        public void Outline(int x, int y, int width, int height, Color color, bool roundCorners)
        {
            int inset = roundCorners ? 1 : 0;
            HLine(x + inset, y, width - 2 * inset, color);
            HLine(x + inset, y + height - 1, width - 2 * inset, color);
            VLine(x, y + inset, height - 2 * inset, color);
            VLine(x + width - 1, y + inset, height - 2 * inset, color);
        }

        /// <summary>지름 diameter의 원. 홀수 지름이면 가운데 픽셀이 (cx, cy).</summary>
        public void FillCircle(float cx, float cy, float radius, Color color) => FillEllipse(cx, cy, radius, radius, color);

        /// <summary>가로 반지름 rx, 세로 반지름 ry의 타원. 픽셀 가운데가 타원 안이면 칠함.</summary>
        public void FillEllipse(float cx, float cy, float rx, float ry, Color color)
        {
            if(rx <= 0f || ry <= 0f)
                return;
            int maxY = Mathf.CeilToInt(cy + ry);
            int maxX = Mathf.CeilToInt(cx + rx);
            for(int y = Mathf.FloorToInt(cy - ry); y <= maxY; y++)
            {
                for(int x = Mathf.FloorToInt(cx - rx); x <= maxX; x++)
                {
                    if(EllipseDistance(x, y, cx, cy, rx, ry) <= 1f)
                        Set(x, y, color);
                }
            }
        }

        /// <summary>타원의 둘레 1픽셀. 바깥 타원(rx, ry) 안쪽, 안쪽 타원(rx-1, ry-1) 밖.</summary>
        public void RingEllipse(float cx, float cy, float rx, float ry, Color color)
        {
            if(rx <= 0f || ry <= 0f)
                return;
            int maxY = Mathf.CeilToInt(cy + ry);
            int maxX = Mathf.CeilToInt(cx + rx);
            for(int y = Mathf.FloorToInt(cy - ry); y <= maxY; y++)
            {
                for(int x = Mathf.FloorToInt(cx - rx); x <= maxX; x++)
                {
                    bool inner = rx > 1f && ry > 1f && EllipseDistance(x, y, cx, cy, rx - 1f, ry - 1f) <= 1f;
                    if(!inner && EllipseDistance(x, y, cx, cy, rx, ry) <= 1f)
                        Set(x, y, color);
                }
            }
        }

        // 픽셀 가운데의 정규화 거리 제곱 (1 이하면 타원 안)
        private static float EllipseDistance(int x, int y, float cx, float cy, float rx, float ry)
        {
            float dx = (x + 0.5f - cx) / rx;
            float dy = (y + 0.5f - cy) / ry;
            return dx * dx + dy * dy;
        }

        public Texture2D ToTexture(string name)
        {
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(_pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
