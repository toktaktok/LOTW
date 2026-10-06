using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Data;

namespace Project.Scripts.Content.Minigame.GateDrop
{
    /// <summary>
    /// 낙하 게이트의 임시 픽셀 그림을 실행 중에 만듭니다. 정식 그림이 생기면 Art/Minigames/GateDrop/ 의 스프라이트로 바꿉니다.
    /// 색이 출구마다 다른 그림(게이트, 버튼, 견본 캔)은 회색조로 그리고 SpriteRenderer.color로 물들입니다.
    /// 피벗 규칙: 가운데 픽셀(크기 / 2)의 왼쪽 아래 모서리. 그래서 정수 픽셀 위치에 놓으면 그림 픽셀이 스테이지 픽셀에 맞음.
    /// </summary>
    public class GateDropArt
    {
        /// <summary>캔 따개 방향 수 (45도 간격).</summary>
        public const int CanFrameCount = 8;
        /// <summary>캔 찌그러짐 단계 (한쪽). 한 단계 = 가로 +2, 세로 -2 픽셀. 음수 단계는 세로로 늘어남.</summary>
        public const int SquashLevels = 1;
        /// <summary>음료 색이 차오르는 단계 수. 그 다음 한 단계(FillSteps + 1)는 테두리까지 덮는 튀어나온 프레임.</summary>
        public const int FillSteps = 4;
        /// <summary>캔이 가장 높이 떴을 때 그림자 지름 비율.</summary>
        public const float ShadowMinScale = 0.6f;

        private static readonly Color Metal = new Color(0.74f, 0.77f, 0.8f);
        private static readonly Color MetalLight = new Color(0.95f, 0.97f, 0.98f);
        private static readonly Color MetalDark = new Color(0.42f, 0.46f, 0.5f);
        /// <summary>판 표면 얼룩 비율(%). 밝은 점, 어두운 점 순.</summary>
        private const int SpeckLightPercent = 3;
        private const int SpeckDarkPercent = 4;

        private readonly List<Object> _owned = new();
        private readonly Sprite[,] _cans = new Sprite[2 * SquashLevels + 1, CanFrameCount];
        // [찌그러짐, 차오름 단계 - 1]
        private readonly Sprite[,] _fills = new Sprite[2 * SquashLevels + 1, FillSteps + 1];
        // 지름 큰 순서 (캔 지름부터 2픽셀씩 작게)
        private readonly Sprite[] _shadows;
        private readonly int _canDiameter;

        public Sprite Board { get; }
        public Sprite Peg { get; }
        public Sprite PegFlash { get; }
        public Sprite DoorLeft { get; }
        public Sprite DoorRight { get; }
        public Sprite Button { get; }
        public Sprite ButtonFrame { get; }
        public Sprite PocketGlow { get; }
        /// <summary>1x1 흰 픽셀. 피벗은 왼쪽 아래 (불똥, 색종이, 연결 빛줄기).</summary>
        public Sprite Pixel { get; }

        public GateDropArt(GateDropLayout layout, GateDropDefinition definition)
        {
            Board = CreateSprite(DrawBoard(layout, definition), "GateDrop_Board", Vector2Int.zero);
            Peg = CreateCentered(DrawPeg(false), "GateDrop_Peg");
            PegFlash = CreateCentered(DrawPeg(true), "GateDrop_PegFlash");

            int door = layout.DoorLength;
            DoorLeft = CreateSprite(DrawDoor(door, true), "GateDrop_DoorLeft", new Vector2Int(0, 1));
            DoorRight = CreateSprite(DrawDoor(door, false), "GateDrop_DoorRight", new Vector2Int(door, 1));

            Button = CreateCentered(DrawButton(layout.ButtonWidth, GateDropDefines.ButtonHeight), "GateDrop_Button");
            ButtonFrame = CreateCentered(DrawFrame(layout.ButtonWidth + 4, GateDropDefines.ButtonHeight + 4), "GateDrop_ButtonFrame");

            int can = layout.CanDiameter;
            _canDiameter = can;
            // 그림자는 홀수 지름만 그려 가운데 픽셀이 캔과 맞게 함 (Transform 배율을 쓰지 않음)
            int minShadow = Mathf.Min(can, Mathf.CeilToInt(can * ShadowMinScale) | 1);
            _shadows = new Sprite[(can - minShadow) / 2 + 1];
            for(int i = 0; i < _shadows.Length; i++)
                _shadows[i] = CreateCentered(DrawDisc(can - 2 * i, Color.white), $"GateDrop_Shadow_{i}");

            for(int squash = -SquashLevels; squash <= SquashLevels; squash++)
            {
                Vector2Int size = new Vector2Int(can + 2 * squash, can - 2 * squash);
                for(int step = 1; step <= FillSteps + 1; step++)
                    _fills[squash + SquashLevels, step - 1] = CreateCentered(DrawFill(size, step), $"GateDrop_Fill_{squash}_{step}");
                for(int i = 0; i < CanFrameCount; i++)
                    _cans[squash + SquashLevels, i] = CreateCentered(DrawCanTop(can, size, i), $"GateDrop_Can_{squash}_{i}");
            }

            PocketGlow = CreateCentered(DrawGlow(layout.LaneWidth - 1, layout.OutletTop - layout.OutletBottom), "GateDrop_PocketGlow");
            PixelCanvas pixel = new PixelCanvas(1, 1);
            pixel.Fill(Color.white);
            Pixel = CreateSprite(pixel, "GateDrop_Pixel", Vector2Int.zero);
        }

        /// <summary>캔 윗면. squash: -SquashLevels..SquashLevels, frame: 따개 방향 (0 = 위, 시계 방향 45도씩).</summary>
        public Sprite GetCan(int squash, int frame) => _cans[squash + SquashLevels, frame];

        /// <summary>착지 후 차오르는 음료 색. 캔 윗면과 같은 찌그러짐 단계. step: 1..FillSteps 차오름, FillSteps + 1 튀어나옴.</summary>
        public Sprite GetFill(int squash, int step) => _fills[squash + SquashLevels, Mathf.Clamp(step, 1, FillSteps + 1) - 1];

        /// <summary>캔 지름 x scale에 가장 가까운 홀수 지름의 그림자 (ShadowMinScale..1).</summary>
        public Sprite GetShadow(float scale)
        {
            int index = Mathf.RoundToInt(_canDiameter * (1f - scale) * 0.5f);
            return _shadows[Mathf.Clamp(index, 0, _shadows.Length - 1)];
        }

        /// <summary>만든 텍스처와 스프라이트를 모두 해제합니다.</summary>
        public void Release()
        {
            foreach(Object owned in _owned)
            {
                if(owned != null)
                    Object.Destroy(owned);
            }
            _owned.Clear();
        }

        #region Board

        private static PixelCanvas DrawBoard(GateDropLayout layout, GateDropDefinition definition)
        {
            Vector2Int size = layout.Size;
            int border = GateDropDefines.Border;
            Color baseColor = definition.BoardColor;
            Color light = Color.Lerp(baseColor, Color.white, 0.2f);
            Color dark = Scale(baseColor, 0.66f);
            Color deep = Scale(baseColor, 0.4f);
            Color speckLight = Color.Lerp(baseColor, Color.white, 0.07f);
            Color speckDark = Scale(baseColor, 0.88f);

            PixelCanvas canvas = new PixelCanvas(size.x, size.y);
            canvas.Fill(baseColor);

            // 판 표면의 성긴 얼룩: 좌표 해시로 흩뿌려 격자나 사선 무늬가 생기지 않게
            for(int y = border; y < size.y - border; y++)
            {
                for(int x = border; x < size.x - border; x++)
                {
                    int roll = Hash(x, y) % 100;
                    if(roll < SpeckLightPercent)
                        canvas.Set(x, y, speckLight);
                    else if(roll < SpeckLightPercent + SpeckDarkPercent)
                        canvas.Set(x, y, speckDark);
                }
            }

            // 테두리: 바깥 1픽셀은 어둡게, 안쪽 1픽셀은 위/왼쪽 밝게 아래/오른쪽 어둡게 (볼록한 판)
            canvas.Outline(0, 0, size.x, size.y, deep, false);
            canvas.HLine(1, size.y - 2, size.x - 2, light);
            canvas.VLine(1, 1, size.y - 2, light);
            canvas.HLine(1, 1, size.x - 2, dark);
            canvas.VLine(size.x - 2, 1, size.y - 2, dark);

            DrawLaneTints(canvas, layout, definition, baseColor);
            DrawInlet(canvas, layout, deep, dark);
            DrawPockets(canvas, layout, definition, deep, dark);
            DrawButtonStrip(canvas, layout, dark, deep);
            DrawPegShadows(canvas, layout, dark);
            DrawGateHinges(canvas, layout);
            return canvas;
        }

        // 게이트 줄 위쪽부터 출구까지 레인마다 음료 색을 아주 옅게 깔아 버튼-게이트-출구가 한 줄로 읽히게 함
        private static void DrawLaneTints(PixelCanvas canvas, GateDropLayout layout, GateDropDefinition definition, Color baseColor)
        {
            int top = layout.RowY(layout.Board.GateRow) + layout.ContactOffset + layout.CanRadius;
            for(int lane = 0; lane < layout.Lanes; lane++)
            {
                Color tint = Color.Lerp(baseColor, definition.Outlets[lane].color, 0.14f);
                int left = layout.LaneX(lane) - layout.HalfLane + 1;
                for(int y = layout.OutletTop; y <= top; y++)
                {
                    // 위쪽 끝은 점점 옅어지게 한 줄 건너 칠함
                    if(y > top - 4 && (y & 1) == 1)
                        continue;
                    canvas.HLine(left, y, layout.LaneWidth - 1, tint);
                }
            }

            // 벽: 출구 바닥부터 첫 줄 위까지
            int wallTop = layout.RowY(0) + layout.ContactOffset;
            canvas.VLine(layout.Left, layout.OutletBottom, wallTop - layout.OutletBottom, MetalDark);
            canvas.VLine(layout.Right, layout.OutletBottom, wallTop - layout.OutletBottom, MetalDark);
        }

        // 핀 그림자: 오른쪽 아래 (캔 그림자와 같은 방향). 게이트 줄은 문짝이라 뺌
        private static void DrawPegShadows(PixelCanvas canvas, GateDropLayout layout, Color shadow)
        {
            GateDropBoard board = layout.Board;
            int half = GateDropDefines.PegSize / 2;
            for(int row = 0; row < board.Rows; row++)
            {
                if(row == board.GateRow)
                    continue;
                for(int column = 0; column <= board.MaxColumn; column++)
                {
                    if(!board.HasPeg(row, column))
                        continue;
                    int x = layout.ColumnX(column);
                    int y = layout.RowY(row);
                    canvas.HLine(x - half + 1, y - half - 1, GateDropDefines.PegSize, shadow);
                    canvas.VLine(x + half + 1, y - half, GateDropDefines.PegSize - 1, shadow);
                }
            }
        }

        // 게이트 경첩 받침: 문짝 바깥 끝에 2픽셀 금속 기둥 (GateDropView의 문짝 위치와 같음)
        private static void DrawGateHinges(PixelCanvas canvas, GateDropLayout layout)
        {
            int y = layout.RowY(layout.Board.GateRow);
            int door = layout.DoorLength;
            for(int lane = 0; lane < layout.Lanes; lane++)
            {
                int x = layout.LaneX(lane);
                canvas.VLine(x - door - 1, y, 2, MetalDark);
                canvas.Set(x - door - 1, y + 1, Metal);
                canvas.VLine(x + door + 1, y, 2, MetalDark);
                canvas.Set(x + door + 1, y + 1, Metal);
            }
        }

        // 투입구: 판 위쪽 가운데의 어두운 홈과 금속 테
        private static void DrawInlet(PixelCanvas canvas, GateDropLayout layout, Color deep, Color dark)
        {
            Vector2 inlet = layout.Inlet;
            int width = layout.CanDiameter + 4;
            int x = (int)inlet.x - width / 2;
            int bottom = (int)inlet.y - layout.CanRadius - 2;
            int height = canvas.Height - GateDropDefines.Border - bottom;
            canvas.FillRect(x, bottom, width, height, deep);
            canvas.VLine(x - 1, bottom, height, Metal);
            canvas.VLine(x + width, bottom, height, MetalDark);
            // 아래 입구 테: 양쪽 끝만 남겨 캔이 빠져나가는 틈을 보여 줌
            canvas.HLine(x - 1, bottom - 1, 3, Metal);
            canvas.HLine(x + width - 2, bottom - 1, 3, MetalDark);
            canvas.HLine(x + 2, bottom, width - 4, dark);
        }

        private static void DrawPockets(PixelCanvas canvas, GateDropLayout layout, GateDropDefinition definition, Color deep, Color dark)
        {
            int height = layout.OutletTop - layout.OutletBottom;
            for(int lane = 0; lane < layout.Lanes; lane++)
            {
                int left = layout.LaneX(lane) - layout.HalfLane + 1;
                int width = layout.LaneWidth - 1;
                canvas.FillRect(left, layout.OutletBottom, width, height, deep);
                // 안쪽 위 그림자, 바닥에는 음료 색 띠
                canvas.HLine(left, layout.OutletTop - 1, width, Scale(deep, 0.7f));
                Color strip = Scale(definition.Outlets[lane].color, 0.55f);
                canvas.HLine(left + 1, layout.OutletBottom, width - 2, strip);
                canvas.HLine(left + 2, layout.OutletBottom + 1, width - 4, Scale(strip, 0.8f));
            }

            // 칸막이: 레인 사이 (홀수 열). 바깥 벽도 같은 높이까지 금속
            for(int column = -1; column <= layout.Board.MaxColumn + 1; column += 2)
            {
                int x = layout.ColumnX(column);
                canvas.VLine(x, layout.OutletBottom, height, Metal);
                canvas.Set(x, layout.OutletBottom - 1, dark);
            }
        }

        // 버튼 줄: 어두운 홈, 버튼마다 눌렸을 때 보이는 받침
        private static void DrawButtonStrip(PixelCanvas canvas, GateDropLayout layout, Color dark, Color deep)
        {
            int border = GateDropDefines.Border;
            canvas.FillRect(border, border, canvas.Width - 2 * border, GateDropDefines.ButtonAreaHeight, dark);
            canvas.HLine(border, border + GateDropDefines.ButtonAreaHeight, canvas.Width - 2 * border, deep);

            int width = layout.ButtonWidth + 2;
            int height = GateDropDefines.ButtonHeight + 1;
            for(int lane = 0; lane < layout.Lanes; lane++)
            {
                int x = layout.LaneX(lane) - width / 2;
                int y = layout.ButtonY - GateDropDefines.ButtonHeight / 2 - 1;
                canvas.FillRect(x, y, width, height, deep);
            }
        }

        #endregion

        #region Pieces

        // 둥근 못 머리 (3x3): 왼쪽 위에서 빛, 모서리는 어둡게 깎아 둥글게 보이게
        private static PixelCanvas DrawPeg(bool flash)
        {
            PixelCanvas canvas = new PixelCanvas(GateDropDefines.PegSize, GateDropDefines.PegSize);
            Color body = flash ? new Color(1f, 0.93f, 0.62f) : Metal;
            Color shade = flash ? new Color(0.95f, 0.66f, 0.28f) : MetalDark;
            Color shine = flash ? Color.white : MetalLight;
            Color rim = flash ? new Color(0.8f, 0.5f, 0.2f) : new Color(0.3f, 0.33f, 0.37f);
            canvas.Fill(body);
            canvas.Set(1, 1, shine);
            canvas.Set(0, 2, shine);
            canvas.Set(2, 1, shade);
            canvas.Set(1, 0, shade);
            canvas.Set(2, 2, body);
            canvas.Set(0, 0, rim);
            canvas.Set(2, 0, rim);
            return canvas;
        }

        // 문짝: 위 줄 밝게, 아래 줄 어둡게, 경첩 끝 픽셀은 더 어둡게
        private static PixelCanvas DrawDoor(int length, bool hingeLeft)
        {
            PixelCanvas canvas = new PixelCanvas(length, GateDropDefines.GateThickness);
            canvas.HLine(0, 1, length, Color.white);
            canvas.HLine(0, 0, length, new Color(0.68f, 0.68f, 0.68f));
            int hinge = hingeLeft ? 0 : length - 1;
            canvas.Set(hinge, 1, new Color(0.55f, 0.55f, 0.55f));
            canvas.Set(hinge, 0, new Color(0.4f, 0.4f, 0.4f));
            return canvas;
        }

        private static PixelCanvas DrawButton(int width, int height)
        {
            PixelCanvas canvas = new PixelCanvas(width, height);
            canvas.FillRect(1, 1, width - 2, height - 2, new Color(0.92f, 0.92f, 0.92f));
            canvas.HLine(1, height - 2, width - 2, Color.white);
            canvas.HLine(1, 1, width - 2, new Color(0.72f, 0.72f, 0.72f));
            canvas.Set(2, height - 3, Color.white);
            canvas.Outline(0, 0, width, height, new Color(0.42f, 0.42f, 0.42f), true);
            return canvas;
        }

        private static PixelCanvas DrawFrame(int width, int height)
        {
            PixelCanvas canvas = new PixelCanvas(width, height);
            canvas.Outline(0, 0, width, height, Color.white, true);
            return canvas;
        }

        private static PixelCanvas DrawDisc(int diameter, Color color)
        {
            PixelCanvas canvas = new PixelCanvas(diameter, diameter);
            float center = diameter * 0.5f;
            canvas.FillCircle(center, center, center, color);
            return canvas;
        }

        // 착지 후 차오르는 음료 색: 가장자리 링을 빼고 안쪽만, step / FillSteps 크기.
        // 마지막 단계(FillSteps + 1)는 테두리까지 덮어 한 번 부푼 것처럼 보이게 함
        private static PixelCanvas DrawFill(Vector2Int size, int step)
        {
            PixelCanvas canvas = new PixelCanvas(size.x, size.y);
            Vector2 center = (Vector2)size * 0.5f;
            Color edge = new Color(0.88f, 0.88f, 0.88f);
            if(step > FillSteps)
            {
                canvas.FillEllipse(center.x, center.y, center.x, center.y, edge);
                canvas.FillEllipse(center.x, center.y, center.x - 1.5f, center.y - 1.5f, Color.white);
                return canvas;
            }

            float ratio = step / (float)FillSteps;
            canvas.FillEllipse(center.x, center.y, (center.x - 1f) * ratio, (center.y - 1f) * ratio, edge);
            canvas.FillEllipse(center.x, center.y, (center.x - 2.5f) * ratio, (center.y - 2.5f) * ratio, Color.white);
            return canvas;
        }

        /// <summary>
        /// 위에서 본 캔 윗면: 테두리, 홈, 따개(방향 frame x 45도), 왼쪽 위 반사광.
        /// size가 지름과 다르면 찌그러진 타원으로 그림 (테두리 두께는 그대로).
        /// </summary>
        private static PixelCanvas DrawCanTop(int diameter, Vector2Int size, int frame)
        {
            PixelCanvas canvas = new PixelCanvas(size.x, size.y);
            Vector2 center = (Vector2)size * 0.5f;
            // 원 위의 점을 타원 위로 옮기는 배율
            Vector2 stretch = (Vector2)size / diameter;
            float radius = diameter * 0.5f;
            canvas.FillEllipse(center.x, center.y, center.x, center.y, new Color(0.5f, 0.5f, 0.5f));
            canvas.FillEllipse(center.x, center.y, center.x - 1f, center.y - 1f, new Color(0.9f, 0.9f, 0.9f));
            canvas.RingEllipse(center.x, center.y, center.x - 1.6f, center.y - 1.6f, new Color(0.72f, 0.72f, 0.72f));
            canvas.FillEllipse(center.x, center.y, center.x - 2.6f, center.y - 2.6f, new Color(0.84f, 0.84f, 0.84f));

            // 따개: 가운데 리벳에서 바깥쪽으로 뻗은 고리
            float angle = Mathf.PI * 0.5f - frame * Mathf.PI * 2f / CanFrameCount;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 side = new Vector2(-direction.y, direction.x);
            float length = Mathf.Max(2f, radius - 2.5f);
            Color tab = new Color(0.62f, 0.62f, 0.62f);
            for(float t = 0f; t <= length; t += 0.25f)
            {
                Vector2 p = direction * t;
                SetStretched(canvas, center, stretch, p, tab);
                if(t > length * 0.45f)
                {
                    SetStretched(canvas, center, stretch, p + side * 0.9f, tab);
                    SetStretched(canvas, center, stretch, p - side * 0.9f, tab);
                }
            }
            SetStretched(canvas, center, stretch, direction * (length * 0.78f), new Color(0.35f, 0.35f, 0.35f));
            canvas.Set((int)center.x, (int)center.y, new Color(0.45f, 0.45f, 0.45f));

            // 반사광: 왼쪽 위 테두리 안쪽 짧은 호 (방향과 무관하게 고정)
            for(float a = 0.62f; a <= 0.95f; a += 0.06f)
            {
                float theta = a * Mathf.PI;
                SetStretched(canvas, center, stretch, new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * (radius - 1.5f), Color.white);
            }
            return canvas;
        }

        // 원 중심 기준 offset을 타원 배율로 늘려 찍음
        private static void SetStretched(PixelCanvas canvas, Vector2 center, Vector2 stretch, Vector2 offset, Color color)
        {
            Vector2 p = center + Vector2.Scale(offset, stretch);
            canvas.Set(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), color);
        }

        // 출구 빛: 테두리는 진하게, 안쪽은 옅게
        private static PixelCanvas DrawGlow(int width, int height)
        {
            PixelCanvas canvas = new PixelCanvas(width | 1, height | 1);
            canvas.FillRect(1, 1, canvas.Width - 2, canvas.Height - 2, new Color(1f, 1f, 1f, 0.28f));
            canvas.Outline(0, 0, canvas.Width, canvas.Height, Color.white, false);
            return canvas;
        }

        #endregion

        private static Color Scale(Color color, float factor) => new Color(color.r * factor, color.g * factor, color.b * factor, 1f);

        // 좌표마다 고정된 0 이상 정수 (판 얼룩이 실행마다 같게)
        private static int Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (int)((h ^ (h >> 16)) & 0x7fffffff);
            }
        }

        private Sprite CreateCentered(PixelCanvas canvas, string name)
        {
            return CreateSprite(canvas, name, new Vector2Int(canvas.Width / 2, canvas.Height / 2));
        }

        private Sprite CreateSprite(PixelCanvas canvas, string name, Vector2Int pivotPixel)
        {
            Texture2D texture = canvas.ToTexture(name);
            Vector2 pivot = new Vector2(pivotPixel.x / (float)canvas.Width, pivotPixel.y / (float)canvas.Height);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, canvas.Width, canvas.Height), pivot,
                CameraDefines.PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            _owned.Add(sprite);
            _owned.Add(texture);
            return sprite;
        }
    }
}
