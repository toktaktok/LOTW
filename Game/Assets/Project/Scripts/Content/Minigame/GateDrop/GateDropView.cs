using UnityEngine;
using Project.Scripts.Data;

namespace Project.Scripts.Content.Minigame.GateDrop
{
    /// <summary>
    /// 낙하 게이트 판의 화면 조립. 스프라이트 오브젝트를 만들고 핀, 게이트, 버튼, 출구, 캔, 입자 연출을 묶어 틱합니다.
    /// 판 전체는 Board 오브젝트 아래에 있어 흔들기(Shake)가 판과 캔을 함께 움직입니다.
    /// </summary>
    public class GateDropView
    {
        private const int BoardOrder = 0;
        private const int GlowOrder = 1;
        private const int SampleOrder = 2;
        private const int PegOrder = 3;
        private const int GateOrder = 4;
        private const int CursorOrder = 5;
        private const int LinkOrder = 6;
        private const int ShadowOrder = 7;
        private const int CanOrder = 8;
        private const int FillOrder = 9;
        private const int ParticleOrder = 10;
        private const int ParticleCount = 64;

        private const float CursorStiffness = 520f;
        private const float CursorDamping = 20f;
        private const float LinkDuration = 0.14f;
        private const float ShakeInterval = 0.035f;
        /// <summary>문짝, 연결 빛줄기 색 = 출구 색을 흰색 쪽으로 이만큼 밝힘.</summary>
        private const float GateTint = 0.25f;
        private const float LinkTint = 0.5f;

        private readonly GateDropLayout _layout;
        private readonly Material _material;
        private readonly Transform _board;
        private readonly GateDropPeg[,] _pegs;
        private readonly SpriteRenderer _cursor;
        private readonly SpriteRenderer _link;
        private SpringValue _cursorX;
        private Color _linkColor;
        private float _linkTime;
        private float _shakeTime;
        private float _shakeTick;
        private int _shakeAmplitude;

        public GateDropGate[] Gates { get; }
        public GateDropButton[] Buttons { get; }
        public GateDropPocket[] Pockets { get; }
        public GateDropCan Can { get; }
        public GateDropParticles Particles { get; }

        public GateDropView(Transform parent, GateDropLayout layout, GateDropArt art, GateDropDefinition definition, Material material)
        {
            _layout = layout;
            _material = material;
            _board = new GameObject("Board").transform;
            _board.SetParent(parent, false);

            SpriteRenderer board = CreateRenderer("Background", art.Board, BoardOrder, _board);
            board.transform.localPosition = layout.ToLocal(Vector2.zero);

            GateDropBoard grid = layout.Board;
            GateDropOutlet[] outlets = definition.Outlets;
            _pegs = new GateDropPeg[grid.Rows, grid.MaxColumn + 1];
            for(int row = 0; row < grid.Rows; row++)
            {
                if(row == grid.GateRow)
                    continue;
                for(int column = 0; column <= grid.MaxColumn; column++)
                {
                    if(!grid.HasPeg(row, column))
                        continue;
                    SpriteRenderer peg = CreateRenderer($"Peg_{row}_{column}", art.Peg, PegOrder, _board);
                    _pegs[row, column] = new GateDropPeg(peg, art.Peg, art.PegFlash, layout, new Vector2(layout.ColumnX(column), layout.RowY(row)));
                }
            }

            int lanes = layout.Lanes;
            Gates = new GateDropGate[lanes];
            Buttons = new GateDropButton[lanes];
            Pockets = new GateDropPocket[lanes];
            int gateY = layout.RowY(grid.GateRow) + 1;
            int door = layout.DoorLength;
            for(int lane = 0; lane < lanes; lane++)
            {
                int x = layout.LaneX(lane);
                Color color = outlets[lane].color;

                SpriteRenderer left = CreateRenderer($"Gate_{lane}_L", art.DoorLeft, GateOrder, _board);
                left.transform.localPosition = layout.ToLocal(new Vector2(x - door, gateY));
                SpriteRenderer right = CreateRenderer($"Gate_{lane}_R", art.DoorRight, GateOrder, _board);
                right.transform.localPosition = layout.ToLocal(new Vector2(x + door + 1, gateY));
                Gates[lane] = new GateDropGate(left, right, Color.Lerp(color, Color.white, GateTint));

                SpriteRenderer button = CreateRenderer($"Button_{lane}", art.Button, GateOrder, _board);
                Buttons[lane] = new GateDropButton(button, layout, new Vector2(x, layout.ButtonY), color);

                SpriteRenderer sample = CreateRenderer($"Sample_{lane}", art.GetCan(0, 0), SampleOrder, _board);
                sample.transform.localPosition = layout.ToLocal(layout.GetPocket(lane));
                SpriteRenderer glow = CreateRenderer($"Glow_{lane}", art.PocketGlow, GlowOrder, _board);
                glow.transform.localPosition = layout.ToLocal(layout.GetPocket(lane));
                Pockets[lane] = new GateDropPocket(sample, glow, color);
            }

            _cursor = CreateRenderer("Cursor", art.ButtonFrame, CursorOrder, _board);
            _cursorX = new SpringValue(layout.LaneX(0));
            _link = CreateRenderer("Link", art.Pixel, LinkOrder, _board);
            _link.enabled = false;

            Can = CreateCan(art, definition.CanColor);

            SpriteRenderer[] particles = new SpriteRenderer[ParticleCount];
            for(int i = 0; i < ParticleCount; i++)
                particles[i] = CreateRenderer($"Particle_{i}", art.Pixel, ParticleOrder, _board);
            Particles = new GateDropParticles(layout, particles);
        }

        /// <summary>커서(버튼 테)를 그 레인으로. instant면 미끄러지지 않고 바로 옮김.</summary>
        public void SetCursor(int lane, bool instant)
        {
            _cursorX.Target = _layout.LaneX(lane);
            if(instant)
            {
                _cursorX.Value = _cursorX.Target;
                _cursorX.Velocity = 0f;
            }
            UpdateCursor();
        }

        public void HideCursor() => _cursor.enabled = false;

        /// <summary>버튼에서 게이트까지 신호가 올라가는 빛줄기.</summary>
        public void Link(int lane, Color color)
        {
            int bottom = _layout.ButtonY + GateDropDefines.ButtonHeight / 2 + 1;
            int top = _layout.RowY(_layout.Board.GateRow) - 1;
            _link.transform.localPosition = _layout.ToLocal(new Vector2(_layout.LaneX(lane), bottom));
            _link.transform.localScale = new Vector3(1f, Mathf.Max(1, top - bottom), 1f);
            _linkColor = Color.Lerp(color, Color.white, LinkTint);
            _linkTime = LinkDuration;
            _link.enabled = true;
        }

        /// <summary>판 전체를 amplitude픽셀 안에서 흔듭니다.</summary>
        public void Shake(float duration, int amplitude)
        {
            _shakeTime = Mathf.Max(_shakeTime, duration);
            _shakeAmplitude = Mathf.Max(1, amplitude);
            _shakeTick = 0f;
        }

        public void HitPeg(int row, int column, int direction)
        {
            if(row >= 0 && row < _pegs.GetLength(0) && column >= 0 && column < _pegs.GetLength(1))
                _pegs[row, column]?.Hit(direction);
        }

        public void Tick(float deltaTime)
        {
            foreach(GateDropPeg peg in _pegs)
                peg?.Tick(deltaTime);
            for(int lane = 0; lane < Gates.Length; lane++)
            {
                Gates[lane].Tick(deltaTime);
                Buttons[lane].Tick(deltaTime);
                Pockets[lane].Tick(deltaTime);
            }
            Can.Tick(deltaTime);
            Particles.Tick(deltaTime);

            _cursorX.Step(deltaTime, CursorStiffness, CursorDamping);
            UpdateCursor();
            TickLink(deltaTime);
            TickShake(deltaTime);
        }

        private void UpdateCursor()
        {
            _cursor.transform.localPosition = _layout.ToLocal(new Vector2(_cursorX.Value, _layout.ButtonY));
        }

        private void TickLink(float deltaTime)
        {
            if(_linkTime <= 0f)
                return;

            _linkTime -= deltaTime;
            Color color = _linkColor;
            color.a = Mathf.Clamp01(_linkTime / LinkDuration);
            _link.color = color;
            if(_linkTime <= 0f)
                _link.enabled = false;
        }

        // 일정 간격마다 정수 픽셀만큼 무작위로 밀고, 끝나면 제자리
        private void TickShake(float deltaTime)
        {
            if(_shakeTime <= 0f)
                return;

            _shakeTime -= deltaTime;
            _shakeTick -= deltaTime;
            if(_shakeTime <= 0f)
            {
                _board.localPosition = Vector3.zero;
                return;
            }
            if(_shakeTick > 0f)
                return;

            _shakeTick = ShakeInterval;
            Vector2 offset = new Vector2(Random.Range(-_shakeAmplitude, _shakeAmplitude + 1), Random.Range(-_shakeAmplitude, _shakeAmplitude + 1));
            _board.localPosition = offset / CameraDefines.PixelsPerUnit;
        }

        private GateDropCan CreateCan(GateDropArt art, Color color)
        {
            Transform root = new GameObject("Can").transform;
            root.SetParent(_board, false);
            SpriteRenderer shadow = CreateRenderer("Shadow", art.GetShadow(1f), ShadowOrder, root);
            SpriteRenderer body = CreateRenderer("Body", art.GetCan(0, 0), CanOrder, root);
            SpriteRenderer fill = CreateRenderer("Fill", art.GetFill(0, GateDropArt.FillSteps), FillOrder, root);
            return new GateDropCan(_layout, root, body, fill, shadow, art, color);
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite, int order, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            if(_material != null)
                renderer.sharedMaterial = _material;
            return renderer;
        }
    }
}
