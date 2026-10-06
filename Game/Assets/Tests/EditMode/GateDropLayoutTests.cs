using NUnit.Framework;
using UnityEngine;
using Project.Scripts.Content.Minigame.GateDrop;
using Project.Scripts.Data;

namespace Tests.EditMode
{
    public class GateDropLayoutTests
    {
        private static readonly Vector2Int[] ExtraSizes =
        {
            new Vector2Int(104, 176),
            new Vector2Int(240, 150),
            new Vector2Int(160, 320),
            // 넓고 낮은 스테이지: 레인을 좁혀야 줄 간격이 나옴
            new Vector2Int(160, 96),
            new Vector2Int(240, 100)
        };

        [Test]
        public void Layout_FitsInsideStage_ForAllOutletCounts()
        {
            for(int lanes = GateDropDefines.MinOutlets; lanes <= GateDropDefines.MaxOutlets; lanes++)
            {
                AssertFits(GateDropLayout.GetMinimumSize(lanes), lanes);
                foreach(Vector2Int size in ExtraSizes)
                    AssertFits(Vector2Int.Max(size, GateDropLayout.GetMinimumSize(lanes)), lanes);
            }
        }

        [Test]
        public void Layout_WideAndShortStage_NarrowsLanes()
        {
            GateDropLayout layout = new GateDropLayout(new Vector2Int(160, 96), 6);

            Assert.Less(layout.LaneWidth, GateDropDefines.MaxLaneWidth);
            Assert.GreaterOrEqual(layout.RowY(0) - layout.RowY(layout.Board.GateRow), GateDropLayout.GetMinRowStep(layout.LaneWidth));
        }

        // Plaza 자판기 크기: 줄 수가 최소 간격 규칙 때문에 바뀌지 않음
        [Test]
        public void Layout_PlazaStage_KeepsElevenRows()
        {
            GateDropLayout layout = new GateDropLayout(new Vector2Int(104, 176), 6);

            Assert.AreEqual(16, layout.LaneWidth);
            Assert.AreEqual(11, layout.Board.Rows);
        }

        [Test]
        public void GetLaneAt_ButtonsAndPockets_ReturnTheirLane()
        {
            GateDropLayout layout = new GateDropLayout(new Vector2Int(104, 176), 6);

            for(int lane = 0; lane < layout.Lanes; lane++)
            {
                Assert.AreEqual(lane, layout.GetLaneAt(new Vector2(layout.LaneX(lane), layout.ButtonY)));
                Assert.AreEqual(lane, layout.GetLaneAt(layout.GetPocket(lane)));
            }
            Assert.AreEqual(-1, layout.GetLaneAt(new Vector2(layout.LaneX(0), layout.OutletTop + 5)));
            Assert.AreEqual(-1, layout.GetLaneAt(new Vector2(layout.Left - 1, layout.ButtonY)));
        }

        [Test]
        public void ToPixel_RoundTripsToLocal()
        {
            GateDropLayout layout = new GateDropLayout(new Vector2Int(103, 175), 6);

            foreach(Vector2 pixel in new[] { Vector2.zero, new Vector2(51, 87), new Vector2(102, 174) })
            {
                Vector2 back = layout.ToPixel(layout.ToLocal(pixel));
                Assert.AreEqual(pixel.x, back.x, 1e-3f);
                Assert.AreEqual(pixel.y, back.y, 1e-3f);
            }
        }

        [Test]
        public void Hop_StartsAndEndsOnPoints_AndPeaksAtHeight()
        {
            GateDropHop hop = new GateDropHop(new Vector2(10f, 50f), new Vector2(18f, 39f), 3.5f, 0.3f);
            Assert.AreEqual(hop.From, hop.Position);

            float peak = float.MinValue;
            for(int i = 0; i <= 300; i++)
            {
                hop.Elapsed = hop.Duration * i / 300f;
                peak = Mathf.Max(peak, hop.Position.y);
            }

            Assert.AreEqual(hop.To.x, hop.Position.x, 1e-4f);
            Assert.AreEqual(hop.To.y, hop.Position.y, 1e-4f);
            Assert.AreEqual(hop.From.y + hop.Height, peak, 0.01f);
        }

        [Test]
        public void Hop_WithoutHeight_OnlyFalls()
        {
            GateDropHop hop = new GateDropHop(new Vector2(0f, 40f), new Vector2(0f, 10f), 0f, 0.2f);
            float previous = hop.Position.y;
            for(int i = 1; i <= 50; i++)
            {
                hop.Elapsed = hop.Duration * i / 50f;
                Assert.LessOrEqual(hop.Position.y, previous + 1e-4f);
                previous = hop.Position.y;
            }
        }

        private static void AssertFits(Vector2Int size, int lanes)
        {
            string label = $"{lanes} lanes, {size}";
            GateDropLayout layout = new GateDropLayout(size, lanes);
            GateDropBoard board = layout.Board;

            Assert.IsTrue(GateDropBoard.IsValidRowCount(lanes, board.Rows), label);
            Assert.GreaterOrEqual(layout.Left, GateDropDefines.Border, label);
            Assert.LessOrEqual(layout.Right, size.x - GateDropDefines.Border - 1, label);
            Assert.GreaterOrEqual(layout.CanDiameter, GateDropDefines.MinCanDiameter, label);
            // 열린 게이트 틈과 출구 칸에 캔이 들어감
            Assert.LessOrEqual(layout.CanDiameter, 2 * layout.DoorLength + 1, label);
            Assert.Less(layout.CanDiameter, layout.LaneWidth - 1, label);

            // 투입구는 판 안, 첫 줄 핀은 투입구 캔보다 아래
            Assert.LessOrEqual(layout.Inlet.y + layout.CanRadius, size.y - GateDropDefines.Border - 1, label);
            Assert.Less(layout.GetContact(0, board.StartColumn).y, layout.Inlet.y - layout.CanRadius, label);

            // 줄은 위에서 아래로. 게이트 줄까지는 한 줄 간격이 최소(반 레인) 이상 (튕길 때마다 눈에 띄게 떨어짐)
            float minStep = GateDropLayout.GetMinRowStep(layout.LaneWidth);
            for(int row = 1; row <= board.GateRow; row++)
                Assert.GreaterOrEqual(layout.RowY(row - 1) - layout.RowY(row), minStep, $"{label}, row {row}");
            Assert.GreaterOrEqual(layout.RowY(board.GateRow) - layout.RowY(board.LastRow), 4, label);
            // 마지막 줄 핀에 얹힌 캔 윗면은 닫힌 게이트 문짝(게이트 줄과 그 위 픽셀, GateDropView)보다 아래
            float lastTop = layout.GetContact(board.LastRow, 1).y + layout.CanRadius;
            Assert.Less(lastTop, layout.RowY(board.GateRow), label);
            Assert.Greater(layout.OutletBottom, layout.ButtonY + GateDropDefines.ButtonHeight / 2, label);
        }
    }
}
