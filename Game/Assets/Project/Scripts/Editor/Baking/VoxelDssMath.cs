// Derived Surface Shading - occupancy grid helper.
// 복셀 점유 필드를 Texture3D 한 채널(R8)로 패킹하기 위한 순수 C# 컨테이너.
// 에디터 전용 어셈블리(Project.Scripts.Editor)에 컴파일되지만 UnityEditor 의존성은 없다.
using UnityEngine;

namespace Project.Scripts.Editor.Baking
{
    /// <summary>
    /// 밀집(dense) 복셀 점유 그리드. 0 = 비어있음, 1 = 채워짐.
    /// </summary>
    public sealed class VoxelOccupancyGrid
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int Depth;
        private readonly byte[] _cells; // x + y*W + z*W*H

        public VoxelOccupancyGrid(int width, int height, int depth)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
            Depth = Mathf.Max(1, depth);
            _cells = new byte[Width * Height * Depth];
        }

        private int Index(int x, int y, int z) => x + y * Width + z * Width * Height;

        public bool InRange(int x, int y, int z)
            => x >= 0 && y >= 0 && z >= 0 && x < Width && y < Height && z < Depth;

        /// <summary>좌표를 채움 상태로 표시. 범위를 벗어나면 무시(경계 안전).</summary>
        public void SetFilled(int x, int y, int z)
        {
            if(!InRange(x, y, z))
                return;
            _cells[Index(x, y, z)] = 255;
        }

        public bool IsFilled(int x, int y, int z)
            => InRange(x, y, z) && _cells[Index(x, y, z)] != 0;

        /// <summary>
        /// Texture3D.SetPixelData에 그대로 넘길 수 있는 R8 바이트 배열.
        /// 텍스처 메모리 레이아웃과 동일하게 x가 가장 빠르게 변한다.
        /// </summary>
        public byte[] ToR8Bytes() => _cells;
    }
}
