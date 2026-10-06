using NUnit.Framework;
using UnityEngine;
using Project.Scripts.System.Minigame;

namespace Tests.EditMode
{
    public class MinigameProjectionTests
    {
        [Test]
        public void ToScreen_AppliesMarginSubPixelAndScale()
        {
            Rect screen = MinigameProjection.ToScreen(new RectInt(10, 20, 30, 40), 1, new Vector2(0.25f, 0.5f), 4);

            Assert.AreEqual(35f, screen.x, 1e-4f);
            Assert.AreEqual(74f, screen.y, 1e-4f);
            Assert.AreEqual(120f, screen.width, 1e-4f);
            Assert.AreEqual(160f, screen.height, 1e-4f);
        }

        [Test]
        public void GetStageRect_CentersOnWholePixels()
        {
            RectInt rect = MinigameProjection.GetStageRect(new Vector2(50.4f, 60.6f), new Vector2Int(20, 30));

            Assert.AreEqual(new RectInt(40, 46, 20, 30), rect);
        }

        [Test]
        public void GetStageResolution_ClampsBetweenMinAndMax()
        {
            Rect pixels = new Rect(0f, 0f, 30.2f, 10f);

            Assert.AreEqual(new Vector2Int(48, 40), MinigameProjection.GetStageResolution(pixels, new Vector2Int(48, 48), new Vector2Int(100, 40)));
            Assert.AreEqual(new Vector2Int(31, 48), MinigameProjection.GetStageResolution(pixels, new Vector2Int(8, 48), Vector2Int.zero));
        }

        [Test]
        public void GetStageResolution_IgnoresFloatErrorAboveWholePixels()
        {
            Rect pixels = new Rect(0f, 0f, 104.0002f, 176.004f);

            Assert.AreEqual(new Vector2Int(104, 176), MinigameProjection.GetStageResolution(pixels, Vector2Int.zero, Vector2Int.zero));
        }
    }
}
