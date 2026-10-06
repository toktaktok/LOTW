using NUnit.Framework;
using UnityEngine;
using Project.Scripts.Content.Minigame.GateDrop;

namespace Tests.EditMode
{
    public class PixelCanvasTests
    {
        [Test]
        public void FillEllipse_FillsExactlyItsBoundingBox()
        {
            PixelCanvas canvas = new PixelCanvas(13, 9);
            canvas.FillEllipse(6.5f, 4.5f, 6.5f, 4.5f, Color.white);

            RectInt bounds = GetPaintedBounds(canvas);
            Assert.AreEqual(new RectInt(0, 0, 13, 9), bounds);
            // 가운데 줄은 끝까지, 모서리는 비어 있음
            Assert.Greater(canvas.Get(0, 4).a, 0f);
            Assert.AreEqual(0f, canvas.Get(0, 0).a);
        }

        [Test]
        public void FillCircle_FiveByFive_LeavesOnlyCornersEmpty()
        {
            PixelCanvas canvas = new PixelCanvas(5, 5);
            canvas.FillCircle(2.5f, 2.5f, 2.5f, Color.white);

            for(int y = 0; y < 5; y++)
            {
                for(int x = 0; x < 5; x++)
                {
                    bool corner = (x == 0 || x == 4) && (y == 0 || y == 4);
                    Assert.AreEqual(corner ? 0f : 1f, canvas.Get(x, y).a, $"({x}, {y})");
                }
            }
        }

        [Test]
        public void RingEllipse_LeavesInsideEmpty()
        {
            PixelCanvas canvas = new PixelCanvas(13, 9);
            canvas.RingEllipse(6.5f, 4.5f, 6.5f, 4.5f, Color.white);

            Assert.AreEqual(0f, canvas.Get(6, 4).a);
            Assert.Greater(canvas.Get(0, 4).a, 0f);
            Assert.Greater(canvas.Get(6, 0).a, 0f);
            Assert.AreEqual(0f, canvas.Get(2, 4).a);
        }

        private static RectInt GetPaintedBounds(PixelCanvas canvas)
        {
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = -1;
            int maxY = -1;
            for(int y = 0; y < canvas.Height; y++)
            {
                for(int x = 0; x < canvas.Width; x++)
                {
                    if(canvas.Get(x, y).a <= 0f)
                        continue;
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
    }
}
