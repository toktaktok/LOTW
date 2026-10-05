// 줄넘기 미니게임 샘플(메카닉 프리팹 + 정의 에셋 + 월드 트리거 프리팹)을 만들고 라이브러리에 등록한다 (에디터 전용).
// 프리팹은 다시 실행하면 덮어쓰고, 정의 에셋은 이미 있으면 건드리지 않는다 (에디터에서 조정한 값 유지).
using Project.Scripts.Content.Minigame;
using Project.Scripts.Content.World;
using Project.Scripts.Data;
using Project.Scripts.System.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Project.Scripts.Editor.Minigame
{
    public static class JumpRopeSampleBuilder
    {
        public const string DefinitionPath = "Assets/Project/Data/Minigames/MinigameDefinition_JumpRope.asset";

        private const string PrefabPath = "Assets/Project/Prefabs/Minigames/PF_Minigame_JumpRope.prefab";
        private const string LineMaterialPath = "Assets/Project/Art/Materials/MAT_MinigameLine.mat";
        private const string LineShaderName = "Sprites/Default";
        private const string JumperSpritePath = "Assets/Project/Art/Characters/001.Snowman/Textures/TX_C_Snowman.png";

        private const string TriggerPrefabPath = "Assets/Project/Prefabs/Minigames/PF_JumpRopeKid.prefab";
        private const string TriggerSpritePath = "Assets/Project/Art/Characters/106.JumpRopeKid/Textures/TX_C_JumpRopeKid.png";
        private const string TriggerPrompt = "줄넘기 하기";
        private const string NpcPrefabPath = "Assets/Project/Prefabs/Characters/PF_NPC_Base.prefab";

        private const string MinigameId = "jumprope";
        private const string Title = "줄넘기";
        private const int SuccessJumps = 5;
        private const int FailMisses = 3;
        private const string RewardActions = "giveItem:rose";

        // 스테이지는 240x150 px = 9.6 x 6 유닛 (PPU 25)
        private const float CameraDistance = 10f;
        private const float CameraFarClip = 30f;
        private const float GroundY = -2f;
        private const float GroundThickness = 2f;
        private const float StageHalfWidth = 5f;
        private const float JumperHalfHeight = 1.6f;
        private const float RopeWidth = 0.08f;
        private const int RopePoints = 17;

        private static readonly Color SkyColor = new Color(0.62f, 0.78f, 0.86f, 1f);
        private static readonly Color GroundColor = new Color(0.47f, 0.62f, 0.42f, 1f);
        private static readonly Color RopeColor = new Color(0.82f, 0.25f, 0.2f, 1f);

        [MenuItem("LOTW/Minigame/2 Build Jump Rope Sample")]
        public static void Run()
        {
            Material lineMaterial = GetOrCreateLineMaterial();
            GameObject prefab = BuildPrefab(lineMaterial);
            JumpRopeDefinition definition = GetOrCreateDefinition(prefab);
            RegisterInLibrary(definition);
            BuildTriggerPrefab(definition);
            AssetDatabase.SaveAssets();
        }

        // 월드에 놓는 줄넘기 아이. PF_NPC_Base와 같은 콜라이더/레이어/스프라이트 설정을 쓴다.
        private static void BuildTriggerPrefab(MinigameDefinition definition)
        {
            var npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath);
            SphereCollider colliderTemplate = npcPrefab.GetComponent<SphereCollider>();
            SpriteRenderer spriteTemplate = npcPrefab.GetComponentInChildren<SpriteRenderer>(true);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TriggerSpritePath);

            var root = new GameObject("PF_JumpRopeKid") { layer = npcPrefab.layer };
            var collider = root.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = colliderTemplate.radius;
            collider.center = colliderTemplate.center;

            var trigger = root.AddComponent<MinigameTrigger>();
            var serialized = new SerializedObject(trigger);
            serialized.FindProperty("definition").objectReferenceValue = definition;
            serialized.FindProperty("promptText").stringValue = TriggerPrompt;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // 스프라이트 피벗이 중앙이므로 발이 루트 위치에 오도록 올림
            var visual = new GameObject("V_JumpRopeKid").AddComponent<SpriteRenderer>();
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, -sprite.bounds.min.y, 0f);
            visual.sprite = sprite;
            visual.sharedMaterial = spriteTemplate.sharedMaterial;
            visual.sortingLayerID = spriteTemplate.sortingLayerID;
            visual.gameObject.AddComponent<BillboardHandler>();

            PrefabUtility.SaveAsPrefabAsset(root, TriggerPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static Material GetOrCreateLineMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(LineMaterialPath);
            if(material != null)
                return material;

            material = new Material(Shader.Find(LineShaderName));
            AssetDatabase.CreateAsset(material, LineMaterialPath);
            return material;
        }

        private static GameObject BuildPrefab(Material lineMaterial)
        {
            var root = new GameObject("PF_Minigame_JumpRope");

            var camera = new GameObject("StageCamera").AddComponent<Camera>();
            camera.transform.SetParent(root.transform, false);
            camera.transform.localPosition = new Vector3(0f, 0f, -CameraDistance);
            camera.orthographic = true;
            camera.orthographicSize = MinigameDefines.DefaultStageResolution.y * 0.5f / CameraDefines.PixelsPerUnit;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SkyColor;
            camera.farClipPlane = CameraFarClip;
            // 월드용 렌더 기능(틸트시프트 등)이 없는 렌더러 사용
            camera.GetUniversalAdditionalCameraData().SetRenderer(CameraDefines.DisplayRendererIndex);

            LineRenderer ground = CreateLine(root.transform, "Ground", lineMaterial, GroundColor, GroundThickness, 2, 0);
            float groundCenter = GroundY - GroundThickness * 0.5f;
            ground.SetPosition(0, new Vector3(-StageHalfWidth, groundCenter, 0f));
            ground.SetPosition(1, new Vector3(StageHalfWidth, groundCenter, 0f));

            var jumper = new GameObject("Jumper").AddComponent<SpriteRenderer>();
            jumper.transform.SetParent(root.transform, false);
            jumper.transform.localPosition = new Vector3(0f, GroundY + JumperHalfHeight, 0f);
            jumper.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(JumperSpritePath);
            jumper.sortingOrder = 1;

            LineRenderer rope = CreateLine(root.transform, "Rope", lineMaterial, RopeColor, RopeWidth, RopePoints, 2);

            var mechanic = root.AddComponent<JumpRopeMinigame>();
            var serialized = new SerializedObject(mechanic);
            serialized.FindProperty("stageCamera").objectReferenceValue = camera;
            serialized.FindProperty("jumper").objectReferenceValue = jumper.transform;
            serialized.FindProperty("rope").objectReferenceValue = rope;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            MinigameWindowPrefabBuilder.EnsureFolder(PrefabPath);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static LineRenderer CreateLine(Transform parent, string name, Material material, Color color, float width, int points, int order)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(parent, false);
            line.useWorldSpace = false;
            line.sharedMaterial = material;
            line.startColor = line.endColor = color;
            line.startWidth = line.endWidth = width;
            line.positionCount = points;
            line.sortingOrder = order;
            return line;
        }

        private static JumpRopeDefinition GetOrCreateDefinition(GameObject prefab)
        {
            var definition = AssetDatabase.LoadAssetAtPath<JumpRopeDefinition>(DefinitionPath);
            if(definition != null)
                return definition;

            MinigameWindowPrefabBuilder.EnsureFolder(DefinitionPath);
            definition = ScriptableObject.CreateInstance<JumpRopeDefinition>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = MinigameId;
            serialized.FindProperty("title").stringValue = Title;
            serialized.FindProperty("mechanicPrefab").objectReferenceValue = prefab;

            SerializedProperty outcomes = serialized.FindProperty("outcomes");
            outcomes.arraySize = 2;
            SetOutcome(outcomes.GetArrayElementAtIndex(0), "success", MinigameOutcomeKind.Success, $"var:jumps>={SuccessJumps}", RewardActions);
            SetOutcome(outcomes.GetArrayElementAtIndex(1), "fail", MinigameOutcomeKind.Fail, $"var:miss>={FailMisses}", string.Empty);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(definition, DefinitionPath);
            return definition;
        }

        private static void SetOutcome(SerializedProperty outcome, string name, MinigameOutcomeKind kind, string when, string actions)
        {
            outcome.FindPropertyRelative("name").stringValue = name;
            outcome.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            outcome.FindPropertyRelative("when").stringValue = when;
            outcome.FindPropertyRelative("actions").stringValue = actions;
            outcome.FindPropertyRelative("followDialogueId").intValue = -1;
        }

        private static void RegisterInLibrary(MinigameDefinition definition)
        {
            var serialized = new SerializedObject(MinigameWindowPrefabBuilder.GetOrCreateLibrary());
            SerializedProperty definitions = serialized.FindProperty("definitions");
            for(int i = 0; i < definitions.arraySize; i++)
            {
                if(definitions.GetArrayElementAtIndex(i).objectReferenceValue == definition)
                    return;
            }

            definitions.arraySize++;
            definitions.GetArrayElementAtIndex(definitions.arraySize - 1).objectReferenceValue = definition;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
