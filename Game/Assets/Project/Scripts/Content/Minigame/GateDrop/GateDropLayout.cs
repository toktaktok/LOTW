using UnityEngine;
using Project.Scripts.Data;

namespace Project.Scripts.Content.Minigame.GateDrop
{
    /// <summary>
    /// 스테이지 크기에 맞춘 낙하 게이트 판의 픽셀 배치 (순수 계산).
    /// 좌표는 스테이지 픽셀, 왼쪽 아래가 (0, 0)입니다. 모든 위치는 "가운데 픽셀"의 정수 좌표이고,
    /// 홀수 크기 그림을 그 픽셀에 가운데 맞춰 그립니다 (GateDropArt의 피벗 규칙과 같음).
    /// 아래에서 위로: 버튼 줄, 출구 칸, 핀 줄들(마지막 줄은 칸막이 위, 끝에서 둘째 줄은 게이트), 투입구.
    /// </summary>
    public class GateDropLayout
    {
        private readonly int[] _rowY;

        public Vector2Int Size { get; }
        public GateDropBoard Board { get; }
        public int Lanes => Board.Lanes;
        public int LaneWidth { get; }
        public int HalfLane => LaneWidth / 2;
        /// <summary>왼쪽 벽 픽셀 x. 오른쪽 벽은 Left + Lanes x LaneWidth.</summary>
        public int Left { get; }
        public int Right => Left + Lanes * LaneWidth;
        public int CanDiameter { get; }
        public int CanRadius => CanDiameter / 2;
        /// <summary>핀(게이트) 중심에서 그 위에 얹힌 캔 중심까지의 높이.</summary>
        public int ContactOffset => GetContactOffset(CanDiameter);
        /// <summary>게이트 문짝 한 쪽 길이. 열리면 문짝 사이로 캔이 지나감.</summary>
        public int DoorLength => Mathf.Max(2, HalfLane - 2);

        public int ButtonY { get; }
        public int ButtonWidth => LaneWidth - 5;
        public int OutletBottom { get; }
        public int OutletTop { get; }
        public int OutletCenterY { get; }
        public int InletY { get; }

        public GateDropLayout(Vector2Int size, int lanes)
        {
            Size = size;

            // 레인 폭은 짝수 (반 레인 = 정수 열 간격). 벽 2개(1픽셀)까지 가로에 들어가게.
            int laneWidth = (size.x - 2 * GateDropDefines.Border - 1) / Mathf.Max(1, lanes);
            laneWidth = Mathf.Clamp(laneWidth - (laneWidth & 1), GateDropDefines.MinLaneWidth, GateDropDefines.MaxLaneWidth);
            // 넓고 낮은 스테이지: 핀 줄 간격이 최소보다 좁아지면 레인을 좁혀 캔과 출구 칸을 줄임
            while(laneWidth > GateDropDefines.MinLaneWidth && !HasRowRoom(size.y, lanes, laneWidth))
                laneWidth -= 2;
            LaneWidth = laneWidth;
            Left = (size.x - (lanes * laneWidth + 1)) / 2;

            CanDiameter = GetCanDiameter(laneWidth);
            ButtonY = GateDropDefines.Border + GateDropDefines.ButtonAreaHeight / 2;
            OutletBottom = GetOutletBottom();
            OutletTop = GetOutletTop(CanDiameter);
            OutletCenterY = OutletBottom + (OutletTop - OutletBottom) / 2;
            InletY = GetInletY(size.y, CanDiameter);

            int firstRow = GetFirstRow(size.y, CanDiameter);
            int gateRow = GetGateRow(CanDiameter);
            int lastRow = OutletTop + 1;

            float span = Mathf.Max(1, firstRow - gateRow);
            int rows = GateDropBoard.GetRowCount(lanes, span, laneWidth * GateDropDefines.RowStepPerLane, GetMinRowStep(laneWidth));
            Board = new GateDropBoard(lanes, rows);

            _rowY = new int[rows];
            int steps = Mathf.Max(1, rows - 2);
            for(int row = 0; row < rows - 1; row++)
                _rowY[row] = Mathf.RoundToInt(Mathf.Lerp(firstRow, gateRow, row / (float)steps));
            _rowY[rows - 1] = lastRow;
        }

        /// <summary>outlets개 출구를 그릴 수 있는 가장 작은 스테이지.</summary>
        public static Vector2Int GetMinimumSize(int outlets)
        {
            int width = outlets * GateDropDefines.MinLaneWidth + 1 + 2 * GateDropDefines.Border;
            return new Vector2Int(width, GateDropDefines.MinStageHeight);
        }

        /// <summary>핀 줄 간격 최소 (스테이지 픽셀).</summary>
        public static float GetMinRowStep(int laneWidth) => laneWidth * GateDropDefines.MinRowStepPerLane;

        // 아래 정적 계산은 레인 폭을 고르기 전에도 쓰므로 인스턴스 값에 기대지 않음

        // 레인 폭에서 버튼 테두리 여백을 뺀 홀수 지름
        private static int GetCanDiameter(int laneWidth)
        {
            return Mathf.Clamp(laneWidth - 5, GateDropDefines.MinCanDiameter, GateDropDefines.MaxCanDiameter) | 1;
        }

        private static int GetContactOffset(int can) => GateDropDefines.PegSize / 2 + 1 + can / 2;

        private static int GetOutletBottom() => GateDropDefines.Border + GateDropDefines.ButtonAreaHeight + 1;

        private static int GetOutletTop(int can) => GetOutletBottom() + can + 2 * GateDropDefines.OutletPadding;

        private static int GetInletY(int height, int can) => height - GateDropDefines.Border - GateDropDefines.InletPadding - can / 2 - 1;

        // 첫 줄 핀: 투입구 캔이 떨어져 얹힐 자리가 투입구 캔 한 개 + 여백만큼 아래에 오도록
        private static int GetFirstRow(int height, int can)
        {
            return GetInletY(height, can) - can - GateDropDefines.InletDropGap - GetContactOffset(can);
        }

        // 마지막 줄(출구 칸 바로 위) 핀에 얹힌 캔이 게이트 문짝 아래에 오도록 게이트 줄을 캔 지름 + 여백만큼 올림
        private static int GetGateRow(int can) => GetOutletTop(can) + 1 + can + GateDropDefines.GateRowGap;

        // 가장 적은 줄로 나눠도 핀 줄 간격이 최소 이상인지
        private static bool HasRowRoom(int height, int lanes, int laneWidth)
        {
            int can = GetCanDiameter(laneWidth);
            return GateDropBoard.HasRoom(lanes, GetFirstRow(height, can) - GetGateRow(can), GetMinRowStep(laneWidth));
        }

        public int RowY(int row) => _rowY[row];

        public int ColumnX(int column) => Left + HalfLane * (column + 1);

        public int LaneX(int lane) => ColumnX(lane * 2);

        /// <summary>(row, column) 핀에 얹힌 캔의 중심.</summary>
        public Vector2 GetContact(int row, int column) => new Vector2(ColumnX(column), RowY(row) + ContactOffset);

        /// <summary>출구 칸에 들어간 캔의 중심.</summary>
        public Vector2 GetPocket(int lane) => new Vector2(LaneX(lane), OutletCenterY);

        public Vector2 Inlet => new Vector2(ColumnX(Board.StartColumn), InletY);

        /// <summary>스테이지 픽셀이 가리키는 버튼(레인). 버튼 줄이나 출구 칸 안이 아니면 -1.</summary>
        public int GetLaneAt(Vector2 pixel)
        {
            if(pixel.y < GateDropDefines.Border || pixel.y >= OutletTop)
                return -1;
            if(pixel.x < Left + 1 || pixel.x >= Right)
                return -1;

            return Mathf.Clamp(Mathf.FloorToInt((pixel.x - Left - 0.5f) / LaneWidth), 0, Lanes - 1);
        }

        /// <summary>스테이지 픽셀 좌표를 스테이지 루트 기준 로컬 위치로 (픽셀 격자에 맞춤).</summary>
        public Vector3 ToLocal(Vector2 pixel, float z = 0f)
        {
            float ppu = CameraDefines.PixelsPerUnit;
            return new Vector3((Mathf.Round(pixel.x) - Size.x * 0.5f) / ppu, (Mathf.Round(pixel.y) - Size.y * 0.5f) / ppu, z);
        }

        /// <summary>스테이지 루트 기준 로컬 위치를 스테이지 픽셀 좌표로.</summary>
        public Vector2 ToPixel(Vector3 local)
        {
            float ppu = CameraDefines.PixelsPerUnit;
            return new Vector2(local.x * ppu + Size.x * 0.5f, local.y * ppu + Size.y * 0.5f);
        }
    }
}
