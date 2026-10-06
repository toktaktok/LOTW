using System;
using NUnit.Framework;
using Project.Scripts.Content.Minigame.GateDrop;

namespace Tests.EditMode
{
    public class GateDropBoardTests
    {
        [Test]
        public void IsValidRowCount_NeedsGateRowOnLaneCenters()
        {
            Assert.IsTrue(GateDropBoard.IsValidRowCount(6, 11));
            Assert.IsFalse(GateDropBoard.IsValidRowCount(6, 10));
            Assert.IsTrue(GateDropBoard.IsValidRowCount(5, 4));
            Assert.IsFalse(GateDropBoard.IsValidRowCount(6, 1));
        }

        [Test]
        public void Constructor_InvalidRows_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GateDropBoard(6, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GateDropBoard(1, 4));
        }

        [Test]
        public void Rows_GateRowUsesLaneCentersAndLastRowUsesDividers()
        {
            GateDropBoard board = new GateDropBoard(6, 11);

            Assert.AreEqual(5, board.StartColumn);
            Assert.AreEqual(10, board.MaxColumn);
            Assert.AreEqual(9, board.GateRow);
            Assert.AreEqual(10, board.LastRow);
            Assert.AreEqual(1, board.GetParity(0));
            Assert.AreEqual(0, board.GetParity(board.GateRow));
            Assert.AreEqual(1, board.GetParity(board.LastRow));
            Assert.IsTrue(board.HasPeg(0, 5));
            Assert.IsFalse(board.HasPeg(0, 4));
            Assert.IsTrue(board.IsGate(9, 4));
            Assert.IsFalse(board.IsGate(9, 5));
            Assert.IsFalse(board.IsGate(8, 4));
        }

        [Test]
        public void Bounce_AtWall_ReflectsInward()
        {
            GateDropBoard board = new GateDropBoard(6, 11);

            Assert.AreEqual(1, board.Bounce(0, false, out bool leftWall));
            Assert.IsTrue(leftWall);
            Assert.AreEqual(9, board.Bounce(10, true, out bool rightWall));
            Assert.IsTrue(rightWall);
            Assert.AreEqual(5, board.Bounce(4, true, out bool noWall));
            Assert.IsFalse(noWall);
        }

        [Test]
        public void GetRowCount_PicksValidCountNearTargetStep()
        {
            int rows = GateDropBoard.GetRowCount(6, 98f, 11.52f, 8f);

            Assert.AreEqual(11, rows);
            Assert.IsTrue(GateDropBoard.IsValidRowCount(6, rows));
        }

        [Test]
        public void GetRowCount_SkipsStepsBelowMinimum_AndFallsBackToFewestRows()
        {
            // 목표 5에 가까운 5줄(간격 6)은 최소 8보다 좁아 3줄(간격 18)을 고름
            Assert.AreEqual(3, GateDropBoard.GetRowCount(6, 18f, 5f, 8f));
            // 어떤 줄 수도 최소를 못 넘으면 가장 적은 유효 줄 수
            Assert.AreEqual(3, GateDropBoard.GetRowCount(6, 4f, 5f, 8f));
            Assert.AreEqual(4, GateDropBoard.GetRowCount(5, 4f, 5f, 8f));
        }

        [Test]
        public void HasRoom_ChecksFewestRowsAgainstMinimumStep()
        {
            Assert.IsTrue(GateDropBoard.HasRoom(6, 18f, 8f));
            // 홀수 레인은 최소 4줄 = 간격 2개
            Assert.IsFalse(GateDropBoard.HasRoom(5, 18f, 10f));
            Assert.IsTrue(GateDropBoard.HasRoom(5, 20f, 10f));
        }

        // 게이트를 열지 않으면 출구 분포는 좌우 대칭이고, 모든 경로는 짝수 열(레인 가운데)에서 끝남
        [Test]
        public void Drop_WithClosedGates_EndsOnLaneCentersSymmetrically([Values(2, 3, 5, 6, 8)] int lanes)
        {
            int rows = lanes % 2 == 0 ? 11 : 10;
            GateDropBoard board = new GateDropBoard(lanes, rows);
            Random random = new Random(1234);
            int left = 0;
            int right = 0;

            for(int drop = 0; drop < 20000; drop++)
            {
                int column = board.StartColumn;
                for(int row = 0; row < board.Rows; row++)
                {
                    Assert.IsTrue(board.HasPeg(row, column), $"row {row} column {column}");
                    column = board.Bounce(column, random.NextDouble() < 0.5, out _);
                }

                Assert.AreEqual(0, column & 1);
                int lane = GateDropBoard.GetLane(column);
                Assert.That(lane, Is.InRange(0, lanes - 1));
                if(lane < lanes / 2)
                    left++;
                else if(lane >= (lanes + 1) / 2)
                    right++;
            }

            float share = left / (float)(left + right);
            Assert.That(share, Is.InRange(0.47f, 0.53f));
        }
    }
}
