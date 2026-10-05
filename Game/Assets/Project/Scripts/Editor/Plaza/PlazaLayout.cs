// Plaza 씬 배치 데이터 (에디터 전용). PlazaSceneBuilder는 이 파일의 값만 읽는다.
// 좌표: X = 거리 방향, Y = 위, +Z = 카메라에서 멀어지는 방향. 레인 A = (Y 0, Z 0), 레인 F = (Y 12, Z 36).
// 스노우맨 스프라이트 박스 약 3.2 유닛(80px, PPU 25) 기준.
using System.Collections.Generic;
using Project.Scripts.Editor.Data;
using UnityEngine;

namespace Project.Scripts.Editor.Plaza
{
    public static class PlazaLayout
    {
        #region Types

        /// <summary>복셀 프리팹 배치. 렌더러 bounds 기준으로 정렬 (min.y = y, center.x = x, min.z = frontZ).</summary>
        public struct Placement
        {
            public string name;
            public string group;
            public string prefabPath;
            public float x;
            public float y;
            public float frontZ;
            public float yaw;
        }

        /// <summary>NPC. dialogueId != 0 이면 PF_NPC_Base 인스턴스(대화 가능), 0 이면 스프라이트만 있는 배경 NPC.</summary>
        public struct NpcEntry
        {
            public string name;
            public int objectId;
            public Vector3 position;
            public string spritePath;
            public string controllerPath;
            public int dialogueId;
            /// <summary>대화/의뢰 테이블 ID. 비우면 "npc_" + 소문자 name.</summary>
            public string characterId;

            public string CharacterId => string.IsNullOrEmpty(characterId) ? "npc_" + name.ToLowerInvariant() : characterId;
        }

        /// <summary>스프라이트 소품. position.y = 스프라이트 아래쪽 끝.</summary>
        public struct SpriteProp
        {
            public string name;
            public string group;
            public string spritePath;
            public string controllerPath;
            public Vector3 position;
        }

        public struct NodeEntry
        {
            public string name;
            public Vector3 position;
        }

        public struct LinkEntry
        {
            public string a;
            public string b;
        }

        public struct ConnectorEntry
        {
            public string name;
            public Vector3 position;
            public string destination;
            public string prompt;
        }

        /// <summary>
        /// 프리미티브 박스. material이 에셋에 없으면 color로 MAT_Placeholder_* 를 만든다.
        /// walkable = Ground 레이어 콜라이더 유지(NavMesh 대상), hidden = 렌더러 제거(콜라이더 전용).
        /// </summary>
        public struct BoxEntry
        {
            public string name;
            public string group;
            public PrimitiveType shape;
            public Vector3 center;
            public Vector3 size;
            public Vector3 euler;
            public string material;
            public Color color;
            public bool walkable;
            public bool hidden;
        }

        public struct ParticleEntry
        {
            public string name;
            public string group;
            public Vector3 position;
        }

        #endregion

        #region Constants

        private const string Chars = "Assets/Project/Art/Characters/";
        private const string Controllers = "Assets/Project/Art/Animations/Controllers/";

        public const string PlayerPrefabPath = "Assets/Project/Prefabs/Characters/PF_SnowMan.prefab";
        public const string NpcPrefabPath = "Assets/Project/Prefabs/Characters/PF_NPC_Base.prefab";
        public const string SpriteMaterialPath = "Assets/Project/Art/Materials/MAT_SpriteShadow.mat";
        public const string BgmPath = "Assets/Project/Audio/BGM/BGM_Village.wav";

        // 그룹 (Generated 루트 아래 경로)
        public const string GroundGroup = "Ground";
        public const string LaneAGroup = "Environment/LaneA";
        public const string SquareGroup = "Environment/Square";
        public const string UpperGroup = "Environment/Upper";
        public const string BackgroundGroup = "Environment/Background";
        public const string StairsLGroup = "Environment/Stairs_L";
        public const string StairsRGroup = "Environment/Stairs_R";
        public const string FountainGroup = "Fountain";
        public const string RailsGroup = "Rails";
        public const string NpcGroup = "NPCs";
        public const string VfxGroup = "VFX";

        public const float LaneFY = 12f;
        public const float LaneFZ = 36f;
        public const string StartNode = "A1";

        // 보정 13: lockYAxis=false는 스프라이트 중심 기준으로 기울어 발이 0.55 유닛 앞으로 나온다. 기본은 yaw만 고정
        public const bool CharacterBillboardLockYAxis = true;

        // 카메라 (C)
        public static readonly Vector2 ComposerScreenPosition = new Vector2(0f, 0.25f);
        public static readonly Vector2 ComposerDeadZone = new Vector2(0.1f, 0.1f);
        public static readonly Vector3 ExploreDamping = new Vector3(1f, 1f, 1f);
        public static readonly Vector3 DialogueDamping = new Vector3(0.5f, 0.5f, 0.5f);
        public const float BrainDefaultBlendTime = 1f;

        // 조명 (A, D1, D4): Character.unity 태양 복사, 고정
        public static readonly Vector3 SunEuler = new Vector3(50f, 333.55f, 0f);
        public static readonly Vector3 SunPosition = new Vector3(0f, 3f, 0f);
        public const float SunIntensity = 2f;
        public const float SunTemperature = 5000f;
        public static readonly Color AmbientSky = Hex("#B9B8E6");
        public static readonly Color AmbientEquator = Hex("#E3D6EA");
        public static readonly Color AmbientGround = Hex("#F2D9DE");

        // 가로등 (D4): 가로등 bounds 위쪽 중앙에서 아래로 LampDrop
        public static readonly Color LampColor = Hex("#FFD48A");
        public const float LampIntensity = 1.5f;
        public const float LampRange = 4f;
        public const float LampDrop = 1.5f;
        public const float LampGlowSize = 0.8f;

        // 벽 타일: 128px / PPU 25
        public const float WallTileUnits = 5.12f;

        // 바닥 범위
        private const float MinX = -60f;
        private const float MaxX = 115f;
        private const float Width = MaxX - MinX;
        private const float MidX = (MinX + MaxX) * 0.5f;

        // 경사로: 레인 A 바닥(Z 4) -> 고원 앞면(Z 30, Y 12)
        private const float RampStartZ = 4f;
        private const float RampEndZ = 30f;
        private const float RampWidth = 3f;
        private const float RampThickness = 0.5f;
        private const float RampOverlap = 1f;
        private const int StairSteps = 12;
        private const float StairWidth = 3.2f;
        public const float RampLX = -36f;
        public const float RampRX = 95f;

        // 분수 (보정 1): 높이 약 14 (두꺼비 포함), 폭 15, 수반 앞면 Z 3
        private const float FountainZ = 9f;
        private const int FountainArcCount = 8;
        private const float ArcThickness = 0.3f;

        #endregion

        #region Placements

        private static string Vox(string relative) => PlazaVoxelPrefabs.PrefabPathFor(ToolDefines.PlazaModelRoot + relative);

        // 레인 A 건물은 높이 <= 12 + (36 - 뒷면Z) * tan20 이어야 레인 F를 가리지 않는다 (보정 8). 빌더가 다시 검사한다.
        // 광장(보정 16): 분수 주변 X -19..+17 을 비워 두고, 광장 안쪽(Z 15-26)에 낮은 건물 2채
        public static readonly Placement[] Placements =
        {
            // --- 레인 A (영상 순서) ---
            new Placement { name = "Building_FarLeft", group = LaneAGroup, prefabPath = Vox("Central/M_E_CommonBuilding_005.vox"), x = -47f, frontZ = 3f },
            new Placement { name = "Building_PaleLeft", group = LaneAGroup, prefabPath = Vox("Central/M_E_CommonBuilding_001.vox"), x = -24f, frontZ = 3f, yaw = 12f },
            new Placement { name = "StreetLight_L", group = LaneAGroup, prefabPath = Vox("Etc/M_E_StreetLight_2.vox"), x = -19f, frontZ = 1f },
            new Placement { name = "Bench_L", group = LaneAGroup, prefabPath = Vox("Etc/M_E_Bench_1.vox"), x = -12f, frontZ = 2.2f },
            new Placement { name = "Bench_R", group = LaneAGroup, prefabPath = Vox("Etc/M_E_Bench_1.vox"), x = 12f, frontZ = 2.2f },
            new Placement { name = "StreetLight_R", group = LaneAGroup, prefabPath = Vox("Etc/M_E_StreetLight_2.vox"), x = 19f, frontZ = 1f },
            new Placement { name = "Bar_Outside", group = LaneAGroup, prefabPath = Vox("Central/M_E_Bar_Outside.vox"), x = 29f, frontZ = 3f },
            // 공중전화(박스, Boxes) X 41.8..44.2. 가로등은 그 오른쪽 (보정 17)
            new Placement { name = "StreetLight_1", group = LaneAGroup, prefabPath = Vox("Etc/M_E_StreetLight_1.vox"), x = 49f, frontZ = 1f },
            new Placement { name = "Building_Setback", group = LaneAGroup, prefabPath = Vox("Central/M_E_CommonBuilding_005.vox"), x = 47f, frontZ = 8f, yaw = -8f },
            new Placement { name = "Bench_Dark", group = LaneAGroup, prefabPath = Vox("Etc/M_E_Bench_2.vox"), x = 58f, frontZ = 1.2f },
            new Placement { name = "Building_Grey", group = LaneAGroup, prefabPath = Vox("Central/M_E_CommonBuilding_001.vox"), x = 69f, frontZ = 3f },
            new Placement { name = "Building_Brick", group = LaneAGroup, prefabPath = Vox("Central/M_E_CommonBuilding_005.vox"), x = 80f, frontZ = 3f },
            new Placement { name = "DrinkMachine", group = LaneAGroup, prefabPath = Vox("Etc/M_E_DrinkMachine_3.vox"), x = 89f, frontZ = 1f },
            // 오른쪽 계단 입구 X 93.5..96.5 는 비워 둠 (보정 9)
            new Placement { name = "Building_RightEnd", group = LaneAGroup, prefabPath = Vox("Central/M_E_CommonBuilding_005.vox"), x = 104f, frontZ = 3f },

            // --- 광장 안쪽 (분수 뒤) ---
            new Placement { name = "Square_ShopL", group = SquareGroup, prefabPath = Vox("Central/M_E_CommonBuilding_001.vox"), x = -13f, frontZ = 15f, yaw = 12f },
            new Placement { name = "Square_ShopR", group = SquareGroup, prefabPath = Vox("Central/M_E_CommonBuilding_005.vox"), x = 13f, frontZ = 17f, yaw = -10f },
            new Placement { name = "Square_BushL", group = SquareGroup, prefabPath = Vox("Etc/M_E_FlowerBush.vox"), x = -9f, frontZ = 3.5f },
            new Placement { name = "Square_BushR", group = SquareGroup, prefabPath = Vox("Etc/M_E_FlowerBush.vox"), x = 9f, frontZ = 3.5f },

            // --- 레인 F (Y 12, 앞면 Z 38.5) ---
            // 돔/배럴 홀(RabbitHouse, 23h)은 광장 안에 두면 레인 F를 가리므로 광장 바로 뒤 레인 F 줄에 둔다
            new Placement { name = "Upper_DomeHall", group = UpperGroup, prefabPath = Vox("Central/M_E_RabbitHouse.vox"), x = 0f, y = LaneFY, frontZ = 38.5f },
            new Placement { name = "Upper_OldApartment", group = UpperGroup, prefabPath = Vox("Middle/M_E_OldApartment.vox"), x = 27f, y = LaneFY, frontZ = 38.5f, yaw = 8f },
            new Placement { name = "Upper_WhiteHouse", group = UpperGroup, prefabPath = Vox("Central/M_E_CommonHouse_001.vox"), x = 50f, y = LaneFY, frontZ = 38.5f },
            new Placement { name = "Upper_PostBox", group = UpperGroup, prefabPath = Vox("Central/M_E_PostBox.vox"), x = 61.5f, y = LaneFY, frontZ = 37.2f },
            new Placement { name = "Upper_Laundry", group = UpperGroup, prefabPath = Vox("Central/M_E_CandyShop.vox"), x = 72f, y = LaneFY, frontZ = 38.5f },
            new Placement { name = "Upper_FlowerShop", group = UpperGroup, prefabPath = ToolDefines.PlazaPrefabFolder + "/PF_FlowerShop.prefab", x = 96f, y = LaneFY, frontZ = 38.5f },

            // --- 배경 (분위기 안개 대상) ---
            new Placement { name = "Far_MiddleHouse", group = BackgroundGroup, prefabPath = Vox("Middle/M_E_MiddleHouse_1.vox"), x = -20f, y = LaneFY, frontZ = 66f, yaw = -6f },
            new Placement { name = "Far_SnowOffice", group = BackgroundGroup, prefabPath = ToolDefines.PlazaPrefabFolder + "/PF_SnowOffice.prefab", x = 30f, y = LaneFY, frontZ = 68f, yaw = 6f },
            new Placement { name = "Far_Apartment", group = BackgroundGroup, prefabPath = Vox("Middle/M_E_OldApartment.vox"), x = 85f, y = LaneFY, frontZ = 66f, yaw = -4f },
        };

        /// <summary>점등 장치(포인트 라이트 + 글로우 쿼드)를 붙일 가로등 배치 이름.</summary>
        public static readonly string[] Lamps = { "StreetLight_L", "StreetLight_R", "StreetLight_1" };

        #endregion

        #region NPCs (E3)

        public static readonly NpcEntry[] Npcs =
        {
            new NpcEntry { name = "Gumman", objectId = 1001, position = new Vector3(0f, 0f, 1f), spritePath = Chars + "101.Gumman/Textures/TX_C_Gumman.png", dialogueId = 1100 },
            new NpcEntry { name = "Catty", objectId = 1002, position = new Vector3(10f, 0f, 1f), spritePath = Chars + "105.Catty/Textures/TX_C_Catty.png", dialogueId = 1200 },
            new NpcEntry { name = "PhoneKids", objectId = 1003, position = new Vector3(24f, 0f, 1f), spritePath = Chars + "131.PhoneKids/Textures/TX_C_PhoneKids.png", dialogueId = 1300 },
            new NpcEntry { name = "Mouse", objectId = 1004, position = new Vector3(55f, 0f, 0.8f), spritePath = Chars + "202.Mouse/Textures/TX_C_Mouse_Idle_1.png", dialogueId = 1400 },
            new NpcEntry { name = "Seal", objectId = 1005, position = new Vector3(66f, 0f, 1f), spritePath = Chars + "128.Seal/Textures/TX_C_Seal.png", dialogueId = 1500 },
            new NpcEntry { name = "Mayor", objectId = 1006, position = new Vector3(52f, LaneFY, LaneFZ + 1f), spritePath = Chars + "108.Mayor/Textures/TX_C_Mayor_Idle.png", controllerPath = Controllers + "AC_Mayor.controller", dialogueId = 1600 },

            new NpcEntry { name = "StarKid", position = new Vector3(22.5f, 0f, 1.5f), spritePath = Chars + "124.PlanetKids/Textures/TX_C_StarKid_Idle_1.png", controllerPath = Controllers + "AC_StarKid.controller" },
            new NpcEntry { name = "PlanetKid", position = new Vector3(27f, 0f, 1.5f), spritePath = Chars + "124.PlanetKids/Textures/TX_C_PlanetKid_Idle.png", controllerPath = Controllers + "AC_PlanetKid.controller" },
            new NpcEntry { name = "Goose", position = new Vector3(36f, 0f, 1.5f), spritePath = Chars + "116.Goose/Textures/TX_C_Goose_Talk.png", controllerPath = Controllers + "AC_Goose.controller" },
            new NpcEntry { name = "Rat", position = new Vector3(84f, 0f, 1.5f), spritePath = Chars + "126.Saus/Textures/TX_C_Saus_Idle.png", controllerPath = Controllers + "AC_Saus.controller" },
            new NpcEntry { name = "Elephant", position = new Vector3(86f, LaneFY, LaneFZ + 1.5f), spritePath = Chars + "112.Ele/Textures/TX_C_Ele1_Idle.png", controllerPath = Controllers + "AC_Ele1.controller" },
            new NpcEntry { name = "Zig", position = new Vector3(38f, LaneFY, LaneFZ + 1.5f), spritePath = Chars + "002.Zig/Textures/TX_C_Zig_Walk.png" },
        };

        public static readonly SpriteProp[] SpriteProps =
        {
            new SpriteProp { name = "Toad", group = FountainGroup, spritePath = Chars + "127.Toad/Textures/TX_C_Toad_Idle.png", controllerPath = Controllers + "AC_Toad.controller", position = new Vector3(0f, 10.7f, FountainZ) },
            new SpriteProp { name = "TrashCan", group = LaneAGroup, spritePath = "Assets/Project/Art/Environments/Props/TX_E_TrashCan_Default.png", position = new Vector3(63.5f, 0f, 1.5f) },
            new SpriteProp { name = "TrashCan_Decal", group = LaneAGroup, spritePath = "Assets/Project/Art/Environments/Decors/TX_E_NumbDumb.png", position = new Vector3(63.5f, 0.6f, 1.45f) },
        };

        #endregion

        #region Rails

        public static readonly NodeEntry[] Nodes =
        {
            new NodeEntry { name = "A0", position = new Vector3(RampLX, 0f, 0f) },
            new NodeEntry { name = "A1", position = new Vector3(-6f, 0f, 0f) },
            new NodeEntry { name = "A2", position = new Vector3(RampRX, 0f, 0f) },
            new NodeEntry { name = "F0", position = new Vector3(RampLX, LaneFY, LaneFZ) },
            new NodeEntry { name = "F1", position = new Vector3(0f, LaneFY, LaneFZ) },
            new NodeEntry { name = "F2", position = new Vector3(RampRX, LaneFY, LaneFZ) },
        };

        public static readonly LinkEntry[] Links =
        {
            new LinkEntry { a = "A0", b = "A1" },
            new LinkEntry { a = "A1", b = "A2" },
            new LinkEntry { a = "F0", b = "F1" },
            new LinkEntry { a = "F1", b = "F2" },
        };

        // 걷기 모드(NavMesh MoveTo). 경로 약 37 유닛 = 속도 5에서 약 7.4초 (타임아웃 10초)
        public static readonly ConnectorEntry[] Connectors =
        {
            new ConnectorEntry { name = "Conn_StairL_Up", position = new Vector3(RampLX, 0f, 0.5f), destination = "F0", prompt = "올라가기" },
            new ConnectorEntry { name = "Conn_StairL_Down", position = new Vector3(RampLX, LaneFY, LaneFZ + 0.5f), destination = "A0", prompt = "내려가기" },
            new ConnectorEntry { name = "Conn_StairR_Up", position = new Vector3(RampRX, 0f, 0.5f), destination = "F2", prompt = "올라가기" },
            new ConnectorEntry { name = "Conn_StairR_Down", position = new Vector3(RampRX, LaneFY, LaneFZ + 0.5f), destination = "A2", prompt = "내려가기" },
        };

        #endregion

        #region Boxes

        public static readonly BoxEntry[] Boxes = BuildBoxes();

        public static readonly ParticleEntry FountainSpray = new ParticleEntry { name = "FX_FountainSpray", group = FountainGroup, position = new Vector3(0f, 10.8f, FountainZ) };
        public static readonly ParticleEntry ChimneySmoke = new ParticleEntry { name = "FX_ChimneySmoke", group = VfxGroup, position = new Vector3(58f, LaneFY + 30f, 64f) };

        private static BoxEntry[] BuildBoxes()
        {
            Color stoneTop = Hex("#A8CBF0");
            var boxes = new List<BoxEntry>
            {
                // --- 바닥 (B Ground) ---
                Walk("Sidewalk", new Vector3(MidX, -2.5f, 0.5f), new Vector3(Width, 5f, 5f), "MAT_PlazaGround"),
                Walk("PlazaFloor", new Vector3(MidX, -0.5f, 16.5f), new Vector3(Width, 1f, 27f), "MAT_PlazaGround"),
                Walk("UpperPlateau", new Vector3(MidX, LaneFY * 0.5f, 60f), new Vector3(Width, LaneFY, 60f), "MAT_PlazaGround"),
                Ramp("Ramp_L", RampLX),
                Ramp("Ramp_R", RampRX),
                Box("Wall_Front", GroundGroup, PrimitiveType.Quad, new Vector3(MidX, -2.5f, -2.01f), new Vector3(Width, 5f, 1f), "MAT_PlazaWall", Color.white),
                Box("Foreground", GroundGroup, PrimitiveType.Cube, new Vector3(MidX, -5.5f, -8f), new Vector3(Width, 1f, 12f), "MAT_Placeholder_ForegroundSnow", Hex("#EEF3FA")),
                Box("Stage", GroundGroup, PrimitiveType.Cylinder, new Vector3(0f, -4f, -6f), new Vector3(12f, 2f, 12f), "MAT_Stage", Hex("#F2E6D8")),
                Box("Stage_Rim", GroundGroup, PrimitiveType.Cylinder, new Vector3(0f, -4.2f, -6f), new Vector3(12.8f, 1.6f, 12.8f), "MAT_Placeholder_StageRim", Hex("#3E8F6A")),

                // --- 소품 플레이스홀더 ---
                Box("PhoneBooth", LaneAGroup, PrimitiveType.Cube, new Vector3(43f, 3f, 2.7f), new Vector3(2.4f, 6f, 2.4f), "MAT_Placeholder_PhoneBooth", Hex("#C8283C")),
                Box("UpperStop_Base", UpperGroup, PrimitiveType.Cylinder, new Vector3(-22f, LaneFY + 2f, 41.5f), new Vector3(6f, 4f, 6f), "MAT_Placeholder_UpperStop", Hex("#B7BCD3")),
                Box("UpperStop_Dome", UpperGroup, PrimitiveType.Sphere, new Vector3(-22f, LaneFY + 4f, 41.5f), new Vector3(6f, 6f, 6f), "MAT_Placeholder_UpperStop", Hex("#B7BCD3")),
                Box("Chimney", BackgroundGroup, PrimitiveType.Cylinder, new Vector3(58f, LaneFY + 15f, 64f), new Vector3(5f, 30f, 5f), "MAT_Placeholder_Chimney", Hex("#8E8A9A")),

                // --- 분수 (단 쌓기) ---
                Box("Basin", FountainGroup, PrimitiveType.Cube, new Vector3(0f, 0.75f, FountainZ), new Vector3(15f, 1.5f, 12f), "MAT_FountainStone", Hex("#6F8FD8")),
                Box("Basin_Water", FountainGroup, PrimitiveType.Cube, new Vector3(0f, 1.45f, FountainZ), new Vector3(13.8f, 0.1f, 10.8f), "MAT_FountainWater", stoneTop),
                Box("Pedestal", FountainGroup, PrimitiveType.Cube, new Vector3(0f, 3.5f, FountainZ), new Vector3(4f, 4f, 4f), "MAT_FountainStone", Hex("#6F8FD8")),
                Box("Bowl_Mid", FountainGroup, PrimitiveType.Cube, new Vector3(0f, 6.1f, FountainZ), new Vector3(9f, 1.2f, 7f), "MAT_FountainStone", Hex("#6F8FD8")),
                Box("Bowl_Mid_Water", FountainGroup, PrimitiveType.Cube, new Vector3(0f, 6.72f, FountainZ), new Vector3(8f, 0.06f, 6f), "MAT_FountainWater", stoneTop),
                Box("Column", FountainGroup, PrimitiveType.Cube, new Vector3(0f, 8.2f, FountainZ), new Vector3(2.5f, 3f, 2.5f), "MAT_FountainStone", Hex("#6F8FD8")),
                Box("Bowl_Top", FountainGroup, PrimitiveType.Cube, new Vector3(0f, 10.2f, FountainZ), new Vector3(5f, 1f, 4f), "MAT_FountainStone", Hex("#6F8FD8")),
            };

            AddStairs(boxes, "Stair_L", StairsLGroup, RampLX);
            AddStairs(boxes, "Stair_R", StairsRGroup, RampRX);
            AddFountainArcs(boxes, stoneTop);
            return boxes.ToArray();
        }

        private static BoxEntry Box(string name, string group, PrimitiveType shape, Vector3 center, Vector3 size, string material, Color color)
        {
            return new BoxEntry { name = name, group = group, shape = shape, center = center, size = size, material = material, color = color };
        }

        private static BoxEntry Walk(string name, Vector3 center, Vector3 size, string material)
        {
            BoxEntry box = Box(name, GroundGroup, PrimitiveType.Cube, center, size, material, Color.white);
            box.walkable = true;
            return box;
        }

        // 콜라이더 전용 경사로. 윗면이 (Z 4, Y 0) -> (Z 30, Y 12) 를 지나고 양 끝을 RampOverlap 만큼 늘려 바닥/고원과 겹친다
        private static BoxEntry Ramp(string name, float x)
        {
            float run = RampEndZ - RampStartZ;
            float angle = Mathf.Atan2(LaneFY, run);
            float length = Mathf.Sqrt(run * run + LaneFY * LaneFY) + RampOverlap;
            Vector3 surfaceMid = new Vector3(x, LaneFY * 0.5f, (RampStartZ + RampEndZ) * 0.5f);
            Vector3 down = new Vector3(0f, -Mathf.Cos(angle), Mathf.Sin(angle)) * (RampThickness * 0.5f);

            BoxEntry box = Box(name, GroundGroup, PrimitiveType.Cube, surfaceMid + down, new Vector3(RampWidth, RampThickness, length), null, Color.white);
            box.euler = new Vector3(-angle * Mathf.Rad2Deg, 0f, 0f);
            box.walkable = true;
            box.hidden = true;
            return box;
        }

        // 경사로를 덮는 계단 모양 박스 (콜라이더 없음). 단 윗면 = 경사로 평균 높이
        private static void AddStairs(List<BoxEntry> boxes, string prefix, string group, float x)
        {
            float run = (RampEndZ - RampStartZ) / StairSteps;
            float rise = LaneFY / StairSteps;
            for(int i = 0; i < StairSteps; i++)
            {
                float top = (i + 0.5f) * rise;
                Vector3 center = new Vector3(x, top * 0.5f, RampStartZ + (i + 0.5f) * run);
                boxes.Add(Box($"{prefix}_{i:00}", group, PrimitiveType.Cube, center, new Vector3(StairWidth, top, run), "MAT_Placeholder_Stairs", Hex("#D9CFE6")));
            }
        }

        // 물줄기: 위 그릇 가장자리 -> 꼭짓점 -> 수반, 2개 마디로 휘어진 느낌
        private static void AddFountainArcs(List<BoxEntry> boxes, Color water)
        {
            for(int i = 0; i < FountainArcCount; i++)
            {
                float a = i * Mathf.PI * 2f / FountainArcCount;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a) * 0.8f);
                Vector3 start = new Vector3(0f, 10.6f, FountainZ) + dir * 2.4f;
                Vector3 apex = new Vector3(0f, 11.8f, FountainZ) + dir * 4.2f;
                Vector3 end = new Vector3(0f, 1.5f, FountainZ) + dir * 6.6f;
                boxes.Add(Segment($"Arc_{i}_Up", start, apex, water));
                boxes.Add(Segment($"Arc_{i}_Down", apex, end, water));
            }
        }

        private static BoxEntry Segment(string name, Vector3 from, Vector3 to, Color color)
        {
            Vector3 delta = to - from;
            BoxEntry box = Box(name, FountainGroup, PrimitiveType.Cube, (from + to) * 0.5f,
                new Vector3(ArcThickness, delta.magnitude, ArcThickness), "MAT_FountainWater", color);
            box.euler = Quaternion.FromToRotation(Vector3.up, delta).eulerAngles;
            return box;
        }

        #endregion

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
