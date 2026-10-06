using System;
using Project.Scripts.Data;

namespace Project.Scripts.Content.Minigame.GateDrop
{
    /// <summary>
    /// 낙하 게이트 판의 칸 규칙 (순수 계산). 캔은 한 줄씩 내려가며 핀에서 왼쪽 아래나 오른쪽 아래로 갑니다.
    /// 가로 위치는 반 레인 단위 열(column)입니다. 짝수 열 = 레인 가운데 (레인 = 열 / 2), 홀수 열 = 두 레인 사이.
    /// 줄마다 짝수 열과 홀수 열이 번갈아 쓰입니다. 0번 줄은 판 가운데(StartColumn)에서 시작합니다.
    /// 끝에서 둘째 줄(GateRow)은 레인 가운데마다 게이트가 있고, 마지막 줄은 출구 칸막이 위의 핀입니다.
    /// 게이트가 열려 있으면 캔이 그 레인의 출구로 바로 떨어지고, 닫혀 있으면 핀처럼 튕깁니다.
    /// </summary>
    public class GateDropBoard
    {
        public int Lanes { get; }
        public int Rows { get; }
        public int StartColumn => Lanes - 1;
        public int MaxColumn => 2 * (Lanes - 1);
        public int GateRow => Rows - 2;
        public int LastRow => Rows - 1;

        public GateDropBoard(int lanes, int rows)
        {
            if(lanes < GateDropDefines.MinOutlets)
                throw new ArgumentOutOfRangeException(nameof(lanes), lanes, "too few lanes");
            if(!IsValidRowCount(lanes, rows))
                throw new ArgumentOutOfRangeException(nameof(rows), rows, "gate row must hold lane centers");

            Lanes = lanes;
            Rows = rows;
        }

        /// <summary>게이트 줄이 레인 가운데(짝수 열)에 오고 마지막 줄이 레인 사이에 오는 줄 수인지.</summary>
        public static bool IsValidRowCount(int lanes, int rows)
        {
            return rows >= GateDropDefines.MinRows && (lanes + rows) % 2 == 1;
        }

        /// <summary>lanes개 레인에서 유효한 가장 적은 줄 수.</summary>
        public static int GetMinRowCount(int lanes)
        {
            return IsValidRowCount(lanes, GateDropDefines.MinRows) ? GateDropDefines.MinRows : GateDropDefines.MinRows + 1;
        }

        /// <summary>span을 가장 적은 줄로 나눠도 간격이 minStep 이상인지.</summary>
        public static bool HasRoom(int lanes, float span, float minStep)
        {
            return span / (GetMinRowCount(lanes) - 2) >= minStep;
        }

        /// <summary>
        /// span(첫 줄 ~ 게이트 줄 거리)을 targetStep에 가장 가까운 간격으로 나누는 유효한 줄 수.
        /// 간격이 minStep보다 좁아지는 줄 수는 고르지 않고, 그런 줄 수뿐이면 가장 적은 줄 수를 돌려줍니다.
        /// 돌려주는 값은 게이트 줄 아래의 마지막 줄까지 센 수라서, span을 나누는 간격 수는 (줄 수 - 2)입니다.
        /// </summary>
        public static int GetRowCount(int lanes, float span, float targetStep, float minStep)
        {
            int best = GetMinRowCount(lanes);
            float bestDiff = float.MaxValue;
            for(int rows = GateDropDefines.MinRows; rows <= GateDropDefines.MaxRows; rows++)
            {
                if(!IsValidRowCount(lanes, rows))
                    continue;

                // 줄이 늘수록 간격은 좁아지기만 함
                float step = span / (rows - 2);
                if(step < minStep)
                    break;

                float diff = Math.Abs(step - targetStep);
                if(diff < bestDiff)
                {
                    bestDiff = diff;
                    best = rows;
                }
            }
            return best;
        }

        /// <summary>그 줄이 쓰는 열인지 (0 = 짝수 열, 1 = 홀수 열).</summary>
        public int GetParity(int row) => (StartColumn + row) & 1;

        public bool HasPeg(int row, int column)
        {
            return row >= 0 && row < Rows && column >= 0 && column <= MaxColumn && (column & 1) == GetParity(row);
        }

        public bool IsGate(int row, int column) => row == GateRow && HasPeg(row, column);

        /// <summary>열이 속한 레인. 짝수 열만 의미가 있습니다.</summary>
        public static int GetLane(int column) => column / 2;

        /// <summary>
        /// (row, column)에서 튕긴 뒤 다음 줄의 열. 판 끝을 넘으면 안쪽으로 튕겨 들어옵니다.
        /// 마지막 줄에서 튕기면 결과는 출구가 있는 짝수 열입니다.
        /// </summary>
        public int Bounce(int column, bool right, out bool hitWall)
        {
            int next = column + (right ? 1 : -1);
            hitWall = next < 0 || next > MaxColumn;
            if(next < 0)
                next = 1;
            else if(next > MaxColumn)
                next = MaxColumn - 1;
            return next;
        }
    }
}
