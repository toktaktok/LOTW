// Plaza 씬 빌더 (에디터 전용). PlazaLayout 데이터로 Plaza.unity를 만든다.
// System / Cameras / Lighting 그룹은 없을 때만 만들고(사용자 튜닝 보존), Generated 루트는 매번 지우고 다시 만든다.
// Cinemachine / URP / AI Navigation / InputSystem / UGUI 타입은 에디터 asmdef가 참조하지 않으므로
// 타입 이름으로 찾고 SerializedObject로 값을 넣는다.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Project.Scripts.Content.Controller;
using Project.Scripts.Content.World;
using Project.Scripts.Data;
using Project.Scripts.Editor.Data;
using Project.Scripts.System.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Project.Scripts.Editor.Plaza
{
    public static class PlazaSceneBuilder
    {
        #region Constants

        private const string SystemGroupName = "--- System ---";
        private const string CamerasGroupName = "--- Cameras ---";
        private const string LightingGroupName = "--- Lighting ---";
        private const string MainCameraName = "Main Camera";
        private const string MainCameraTag = "MainCamera";
        private const string EventSystemName = "EventSystem";
        private const string SceneStarterName = "[SceneStarter]";
        private const string PlayerName = "Player";
        private const string SnowManName = "SnowMan";
        private const string VCamPlazaName = "VCam_Plaza";
        private const string VCamDialogueName = "VCam_Dialogue";
        private const string SunName = "Directional Light";
        private const string VolumeName = "Global Volume";
        private const string SnowName = "FX_Snow";
        private const string LampName = "Lamp";
        private const string InteractableLayerName = "Interactable";
        private const string GroundLayerName = "Ground";
        private const string PlaceholderPrefix = "MAT_Placeholder_";
        private const string SkyMaterialName = "MAT_Sky_Plaza";
        private const string LampGlowMaterialName = "MAT_LampGlow";
        private const string SnowMaterialName = "MAT_Snow";
        private const string LitShaderName = "Universal Render Pipeline/Lit";

        private const string CinemachineCameraType = "Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine";
        private const string PositionComposerType = "Unity.Cinemachine.CinemachinePositionComposer, Unity.Cinemachine";
        private const string BrainType = "Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine";
        private const string UrpCameraDataType = "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime";
        private const string VolumeType = "UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime";
        private const string EventSystemType = "UnityEngine.EventSystems.EventSystem, UnityEngine.UI";
        private const string InputModuleType = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";
        private const string NavMeshSurfaceType = "Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation";
        private const string NavMeshAssetManagerType = "Unity.AI.Navigation.Editor.NavMeshAssetManager, Unity.AI.Navigation.Editor";

        // Character.unity 메인 카메라 / vcam 렌즈와 같은 값
        private const float CameraNearClip = -100f;
        private const float CameraFarClip = 1000f;
        private const int BlendStyleEaseInOut = 1;      // CinemachineBlendDefinition.Styles.EaseInOut
        private const int LensModeOrthographic = 1;     // LensSettings.OverrideModes.Orthographic
        private const int CollectChildren = 2;          // CollectObjects.Children
        private const int CollectPhysicsColliders = 1;  // NavMeshCollectGeometry.PhysicsColliders

        private const float ConnectorTriggerRadius = 0.5f;
        private const float GlowZOffset = 0.05f;

        // 레인 F 가림 검사 (보정 8)
        private const float OcclusionSampleStep = 2f;
        private const float OcclusionFeetOffset = 0.5f;

        // D6 파티클
        private static readonly Vector3 SnowLocalPosition = new Vector3(0f, 12f, 10f);
        private static readonly Vector3 SnowBoxSize = new Vector3(44f, 1f, 30f);
        private const float SnowLifetime = 9f;
        private const float SnowGravity = 0.07f;
        private const float SnowRate = 80f;
        private const int SnowMaxParticles = 1000;
        private const float SnowSizeMin = 0.04f;
        private const float SnowSizeMax = 0.08f;
        private const float SnowDrift = 0.2f;
        private const float SnowNoise = 0.3f;

        private const float SmokeAngle = 10f;
        private const float SmokeRadius = 1f;
        private const float SmokeLifetime = 6f;
        private const float SmokeSpeed = 0.5f;
        private const float SmokeSizeMin = 1.5f;
        private const float SmokeSizeMax = 3f;
        private const float SmokeRate = 4f;
        private static readonly Color SmokeColor = new Color(0.6f, 0.63f, 0.69f);

        private static readonly Vector3 EmitUp = new Vector3(-90f, 0f, 0f);

        #endregion

        #region Build

        [MenuItem("LOTW/Plaza/5 Build Scene")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)
            {
                Debug.LogError("[PlazaSceneBuilder] Exit Play mode first");
                return;
            }

            string playerPrefabPath = PlazaPlayerPrefab.Create();
            if(playerPrefabPath == null)
                return;

            if(!TryOpenPlazaScene(out Scene scene))
                return;

            Transform system = GetOrCreateRoot(scene, SystemGroupName);
            Transform cameras = GetOrCreateRoot(scene, CamerasGroupName);
            Transform lighting = GetOrCreateRoot(scene, LightingGroupName);

            GameObject snowMan = GetOrCreateSnowMan(system, playerPrefabPath);
            GetOrCreateMainCamera(system, snowMan.transform);
            GetOrCreateEventSystem(system);
            Component vcamPlaza = GetOrCreateVCam(cameras, VCamPlazaName, snowMan.transform,
                ToolDefines.PlazaExploreOrthoSize, PlazaLayout.ExploreDamping);
            Component vcamDialogue = GetOrCreateVCam(cameras, VCamDialogueName, snowMan.transform,
                ToolDefines.PlazaDialogueOrthoSize, PlazaLayout.DialogueDamping);
            GetOrCreateSceneStarter(system, vcamPlaza);
            Light sun = GetOrCreateLighting(lighting);
            ApplyRenderSettings(sun);

            Transform generated = RebuildGeneratedRoot(scene);
            BuildBoxes(generated);
            Dictionary<string, GameObject> placed = BuildPlacements(generated);
            BuildLamps(placed);
            BuildSpriteProps(generated);
            Dictionary<string, RailNode> nodes = BuildRails(generated);
            BuildNpcs(generated, vcamDialogue);
            CreateSmoke(GetGroup(generated, PlazaLayout.ChimneySmoke.group), PlazaLayout.ChimneySmoke);
            WirePlayer(system, snowMan, nodes[PlazaLayout.StartNode]);

            CheckLaneFOcclusion(generated);
            CheckRampFootprints(generated);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddToBuildSettings();
        }

        /// <summary>
        /// NavMesh를 비동기로 굽는다 (보정 5). 씬을 먼저 저장하고(에셋 경로가 씬 경로에서 정해짐),
        /// 굽기가 끝나면 EditorApplication.update에서 다시 저장한다. 호출은 바로 반환된다.
        /// </summary>
        [MenuItem("LOTW/Plaza/6 Bake NavMesh")]
        public static void BakeNavMesh()
        {
            if(EditorApplication.isPlaying)
            {
                Debug.LogError("[PlazaSceneBuilder] Exit Play mode first");
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(ToolDefines.PlazaScenePath);
            if(!scene.isLoaded && !TryOpenPlazaScene(out scene))
                return;

            Type surfaceType = FindType(NavMeshSurfaceType);
            Type managerType = FindType(NavMeshAssetManagerType);
            if(surfaceType == null || managerType == null)
                return;

            Component surface = FindInScene(scene, surfaceType);
            if(surface == null)
            {
                Debug.LogError("[PlazaSceneBuilder] No NavMeshSurface in Plaza. Run 'LOTW/Plaza/5 Build Scene' first");
                return;
            }

            EditorSceneManager.SaveScene(scene);

            object manager = managerType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetValue(null);
            MethodInfo startBaking = managerType.GetMethod("StartBakingSurfaces");
            MethodInfo isBaking = managerType.GetMethod("IsSurfaceBaking");
            if(manager == null || startBaking == null || isBaking == null)
            {
                Debug.LogError("[PlazaSceneBuilder] NavMeshAssetManager API not found. Bake the Ground NavMeshSurface manually");
                return;
            }

            startBaking.Invoke(manager, new object[] { new Object[] { surface } });

            EditorApplication.CallbackFunction poll = null;
            poll = () =>
            {
                if(surface != null && (bool)isBaking.Invoke(manager, new object[] { surface }))
                    return;

                EditorApplication.update -= poll;
                if(scene.isLoaded)
                    EditorSceneManager.SaveScene(scene);
            };
            EditorApplication.update += poll;
        }

        #endregion

        #region Scene

        // 열려 있는 Plaza에 저장 안 된 변경이 있거나, 씬을 바꾸면 다른 씬의 변경이 사라지는 경우 거부한다
        private static bool TryOpenPlazaScene(out Scene scene)
        {
            string path = ToolDefines.PlazaScenePath;
            scene = SceneManager.GetSceneByPath(path);
            if(scene.isLoaded)
            {
                if(scene.isDirty)
                {
                    Debug.LogError($"[PlazaSceneBuilder] {path} has unsaved changes. Save or revert it first");
                    return false;
                }
                SceneManager.SetActiveScene(scene);
                return true;
            }

            for(int i = 0; i < SceneManager.sceneCount; i++)
            {
                if(SceneManager.GetSceneAt(i).isDirty)
                {
                    Debug.LogError($"[PlazaSceneBuilder] Scene '{SceneManager.GetSceneAt(i).name}' has unsaved changes. Save it before opening Plaza");
                    return false;
                }
            }

            if(File.Exists(path))
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                return true;
            }

            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if(!EditorSceneManager.SaveScene(scene, path))
            {
                Debug.LogError($"[PlazaSceneBuilder] Failed to create {path}");
                return false;
            }
            return true;
        }

        private static void AddToBuildSettings()
        {
            string path = ToolDefines.PlazaScenePath;
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if(scenes.Exists(s => s.path == path))
                return;

            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach(GameObject root in scene.GetRootGameObjects())
            {
                if(root.name == name)
                    return root;
            }
            return null;
        }

        private static Transform GetOrCreateRoot(Scene scene, string name)
        {
            GameObject root = FindRoot(scene, name);
            return root != null ? root.transform : new GameObject(name).transform;
        }

        private static Component FindInScene(Scene scene, Type type)
        {
            foreach(GameObject root in scene.GetRootGameObjects())
            {
                Component found = root.GetComponentInChildren(type, true);
                if(found != null)
                    return found;
            }
            return null;
        }

        private static Transform RebuildGeneratedRoot(Scene scene)
        {
            GameObject old = FindRoot(scene, ToolDefines.PlazaGeneratedRootName);
            if(old != null)
            {
                DeleteNavMeshAsset(old);
                Object.DestroyImmediate(old);
            }
            return new GameObject(ToolDefines.PlazaGeneratedRootName).transform;
        }

        // 다시 만든 Ground는 데이터가 비어 있어 이전 NavMesh 에셋이 고아가 되므로 미리 지운다
        private static void DeleteNavMeshAsset(GameObject oldRoot)
        {
            Type surfaceType = FindType(NavMeshSurfaceType);
            if(surfaceType == null)
                return;

            Component surface = oldRoot.GetComponentInChildren(surfaceType, true);
            if(surface == null)
                return;

            SerializedProperty data = new SerializedObject(surface).FindProperty("m_NavMeshData");
            string assetPath = data != null ? AssetDatabase.GetAssetPath(data.objectReferenceValue) : null;
            if(!string.IsNullOrEmpty(assetPath))
                AssetDatabase.DeleteAsset(assetPath);
        }

        private static Transform GetGroup(Transform root, string path)
        {
            Transform current = root;
            foreach(string part in path.Split('/'))
            {
                Transform next = current.Find(part);
                if(next == null)
                {
                    next = new GameObject(part).transform;
                    next.SetParent(current, false);
                }
                current = next;
            }
            return current;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        #endregion

        #region System, Cameras, Lighting

        private static GameObject GetOrCreateSnowMan(Transform system, string prefabPath)
        {
            Transform existing = system.Find(SnowManName);
            GameObject snowMan;
            if(existing != null)
                snowMan = existing.gameObject;
            else
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                snowMan = (GameObject)PrefabUtility.InstantiatePrefab(prefab, system);
                snowMan.name = SnowManName;
                snowMan.transform.position = GetNodePosition(PlazaLayout.StartNode);
            }

            SetProperties(snowMan.GetComponentInChildren<BillboardHandler>(true), ("lockYAxis", PlazaLayout.CharacterBillboardLockYAxis));
            return snowMan;
        }

        private static void GetOrCreateMainCamera(Transform system, Transform follow)
        {
            Transform existing = system.Find(MainCameraName);
            GameObject go;
            if(existing != null)
                go = existing.gameObject;
            else
            {
                go = CreateChild(system, MainCameraName);
                PlaceBehind(go.transform, follow);

                var camera = go.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = ToolDefines.PlazaExploreOrthoSize;
                camera.nearClipPlane = CameraNearClip;
                camera.farClipPlane = CameraFarClip;
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.allowHDR = true;
                go.AddComponent<AudioListener>();

                SetProperties(AddComponent(go, UrpCameraDataType), ("m_RenderPostProcessing", true), ("m_Antialiasing", 0));
                SetProperties(AddComponent(go, BrainType),
                    ("LensModeOverride.Enabled", true),
                    ("LensModeOverride.DefaultMode", LensModeOrthographic),
                    ("DefaultBlend.Style", BlendStyleEaseInOut),
                    ("DefaultBlend.Time", PlazaLayout.BrainDefaultBlendTime));
                go.AddComponent<LowResPixelRenderer>();
            }

            // CameraManager가 Camera.main을 쓰므로 태그는 항상 맞춘다 (보정 10)
            go.tag = MainCameraTag;
            if(go.transform.Find(SnowName) == null)
                CreateSnow(go.transform);
        }

        private static void GetOrCreateEventSystem(Transform system)
        {
            if(system.Find(EventSystemName) != null)
                return;

            GameObject go = CreateChild(system, EventSystemName);
            AddComponent(go, EventSystemType);
            AddComponent(go, InputModuleType);
        }

        private static Component GetOrCreateVCam(Transform cameras, string name, Transform follow, float orthoSize, Vector3 damping)
        {
            Transform existing = cameras.Find(name);
            if(existing != null)
            {
                Component vcam = AddComponent(existing.gameObject, CinemachineCameraType);
                SerializedProperty target = vcam != null ? new SerializedObject(vcam).FindProperty("Target.TrackingTarget") : null;
                if(target != null && target.objectReferenceValue == null)
                    SetProperties(vcam, ("Target.TrackingTarget", follow));
                return vcam;
            }

            GameObject go = CreateChild(cameras, name);
            PlaceBehind(go.transform, follow);

            Component camera = AddComponent(go, CinemachineCameraType);
            SetProperties(camera,
                ("Priority.Enabled", true),
                ("Priority.m_Value", CameraDefines.DefaultCameraPriority),
                ("Lens.OrthographicSize", orthoSize),
                ("Lens.NearClipPlane", CameraNearClip),
                ("Lens.FarClipPlane", CameraFarClip),
                ("Target.TrackingTarget", follow));
            SetProperties(AddComponent(go, PositionComposerType),
                ("CameraDistance", ToolDefines.PlazaCameraDistance),
                ("Damping", damping),
                ("Composition.ScreenPosition", PlazaLayout.ComposerScreenPosition),
                ("Composition.DeadZone.Enabled", true),
                ("Composition.DeadZone.Size", PlazaLayout.ComposerDeadZone));
            return camera;
        }

        private static void PlaceBehind(Transform transform, Transform follow)
        {
            transform.rotation = Quaternion.Euler(ToolDefines.PlazaCameraPitch, 0f, 0f);
            transform.position = follow.position + transform.rotation * (Vector3.back * ToolDefines.PlazaCameraDistance);
        }

        private static void GetOrCreateSceneStarter(Transform system, Component defaultCamera)
        {
            Transform existing = system.Find(SceneStarterName);
            GameObject go = existing != null ? existing.gameObject : CreateChild(system, SceneStarterName);

            var cameraStarter = go.GetComponent<SceneCameraStarter>();
            if(cameraStarter == null)
            {
                cameraStarter = go.AddComponent<SceneCameraStarter>();
                SetProperties(cameraStarter, ("sceneDefaultCamera", defaultCamera));
            }

            if(go.GetComponent<SceneBgm>() == null)
                SetProperties(go.AddComponent<SceneBgm>(), ("bgm", AssetDatabase.LoadAssetAtPath<AudioClip>(PlazaLayout.BgmPath)));
        }

        private static Light GetOrCreateLighting(Transform lighting)
        {
            Transform sunTransform = lighting.Find(SunName);
            Light sun;
            if(sunTransform != null)
                sun = sunTransform.GetComponent<Light>();
            else
            {
                GameObject go = CreateChild(lighting, SunName);
                go.transform.SetPositionAndRotation(PlazaLayout.SunPosition, Quaternion.Euler(PlazaLayout.SunEuler));
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = PlazaLayout.SunIntensity;
                sun.useColorTemperature = true;
                sun.colorTemperature = PlazaLayout.SunTemperature;
                sun.shadows = LightShadows.Hard;
            }

            Transform volumeTransform = lighting.Find(VolumeName);
            GameObject volumeObject = volumeTransform != null ? volumeTransform.gameObject : CreateChild(lighting, VolumeName);
            Component volume = AddComponent(volumeObject, VolumeType);
            SerializedProperty profile = volume != null ? new SerializedObject(volume).FindProperty("sharedProfile") : null;
            if(profile != null && profile.objectReferenceValue == null)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(ToolDefines.PlazaVolumeProfilePath);
                if(asset == null)
                    Debug.LogWarning($"[PlazaSceneBuilder] {ToolDefines.PlazaVolumeProfilePath} missing. Run 'LOTW/Plaza/2 Setup Render' and rebuild");
                SetProperties(volume, ("m_IsGlobal", true), ("sharedProfile", asset));
            }
            return sun;
        }

        private static void ApplyRenderSettings(Light sun)
        {
            var sky = AssetDatabase.LoadAssetAtPath<Material>($"{ToolDefines.PlazaMaterialFolder}/{SkyMaterialName}.mat");
            if(sky == null)
                Debug.LogWarning($"[PlazaSceneBuilder] {SkyMaterialName} missing. Run 'LOTW/Plaza/2 Setup Render' and rebuild");
            else
                RenderSettings.skybox = sky;

            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = PlazaLayout.AmbientSky;
            RenderSettings.ambientEquatorColor = PlazaLayout.AmbientEquator;
            RenderSettings.ambientGroundColor = PlazaLayout.AmbientGround;
            RenderSettings.fog = false;
        }

        private static void WirePlayer(Transform system, GameObject snowMan, RailNode startNode)
        {
            Transform existing = system.Find(PlayerName);
            GameObject go = existing != null ? existing.gameObject : CreateChild(system, PlayerName);

            var controller = go.GetComponent<PlayerController>();
            if(controller == null)
                controller = go.AddComponent<PlayerController>();
            var interactor = go.GetComponent<PlayerInteractor>();
            if(interactor == null)
                interactor = go.AddComponent<PlayerInteractor>();

            var character = snowMan.GetComponent<Character>();
            SetProperties(controller, ("currentCharacter", character), ("currentBaseNode", startNode), ("currentTargetNode", null));
            SetProperties(interactor, ("currentCharacter", character), ("interactableLayerMask", 1 << LayerMask.NameToLayer(InteractableLayerName)));
        }

        #endregion

        #region Generated

        private static void BuildBoxes(Transform root)
        {
            int groundLayer = LayerMask.NameToLayer(GroundLayerName);
            foreach(PlazaLayout.BoxEntry box in PlazaLayout.Boxes)
            {
                GameObject go = GameObject.CreatePrimitive(box.shape);
                go.name = box.name;
                go.transform.SetParent(GetGroup(root, box.group), false);
                go.transform.localPosition = box.center;
                go.transform.localRotation = Quaternion.Euler(box.euler);
                // 기본 실린더 높이는 2
                go.transform.localScale = box.shape == PrimitiveType.Cylinder
                    ? new Vector3(box.size.x, box.size.y * 0.5f, box.size.z)
                    : box.size;

                if(box.walkable)
                    go.layer = groundLayer;
                else
                    Object.DestroyImmediate(go.GetComponent<Collider>());

                if(box.hidden)
                {
                    Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
                    Object.DestroyImmediate(go.GetComponent<MeshFilter>());
                    continue;
                }

                Material material = GetMaterial(box.material, box.color);
                go.GetComponent<MeshRenderer>().sharedMaterial = material;
                if(box.shape == PrimitiveType.Quad && material != null)
                    material.mainTextureScale = new Vector2(box.size.x / PlazaLayout.WallTileUnits, box.size.y / PlazaLayout.WallTileUnits);
            }

            Transform ground = GetGroup(root, PlazaLayout.GroundGroup);
            SetProperties(AddComponent(ground.gameObject, NavMeshSurfaceType),
                ("m_CollectObjects", CollectChildren),
                ("m_UseGeometry", CollectPhysicsColliders),
                ("m_LayerMask", 1 << groundLayer));
        }

        private static Dictionary<string, GameObject> BuildPlacements(Transform root)
        {
            var placed = new Dictionary<string, GameObject>();
            foreach(PlazaLayout.Placement placement in PlazaLayout.Placements)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(placement.prefabPath);
                if(prefab == null)
                {
                    Debug.LogWarning($"[PlazaSceneBuilder] Prefab missing, skipped {placement.name}: {placement.prefabPath}");
                    continue;
                }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, GetGroup(root, placement.group));
                go.name = placement.name;
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, placement.yaw, 0f));
                if(TryGetBounds(go, out Bounds bounds))
                    go.transform.position = new Vector3(placement.x - bounds.center.x, placement.y - bounds.min.y, placement.frontZ - bounds.min.z);
                else
                {
                    Debug.LogWarning($"[PlazaSceneBuilder] {placement.name} has no renderers, placed by pivot");
                    go.transform.position = new Vector3(placement.x, placement.y, placement.frontZ);
                }
                placed[placement.name] = go;
            }
            return placed;
        }

        // 가로등 bounds 위쪽 중앙 앞면에 포인트 라이트 + 글로우 쿼드 (D4)
        private static void BuildLamps(Dictionary<string, GameObject> placed)
        {
            Material glow = GetMaterial(LampGlowMaterialName, PlazaLayout.LampColor);
            foreach(string name in PlazaLayout.Lamps)
            {
                if(!placed.TryGetValue(name, out GameObject streetLight) || !TryGetBounds(streetLight, out Bounds bounds))
                    continue;

                var lamp = new GameObject(LampName);
                lamp.transform.SetParent(streetLight.transform, false);
                lamp.transform.position = new Vector3(bounds.center.x, bounds.max.y - PlazaLayout.LampDrop, bounds.min.z - GlowZOffset);
                lamp.transform.rotation = Quaternion.identity;

                var light = lamp.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = PlazaLayout.LampColor;
                light.intensity = PlazaLayout.LampIntensity;
                light.range = PlazaLayout.LampRange;
                light.shadows = LightShadows.None;

                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Glow";
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.transform.SetParent(lamp.transform, false);
                quad.transform.localScale = Vector3.one * PlazaLayout.LampGlowSize;
                var quadRenderer = quad.GetComponent<MeshRenderer>();
                quadRenderer.sharedMaterial = glow;
                quadRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private static void BuildSpriteProps(Transform root)
        {
            foreach(PlazaLayout.SpriteProp prop in PlazaLayout.SpriteProps)
                CreateSpriteObject(GetGroup(root, prop.group), prop.name, prop.spritePath, prop.controllerPath, prop.position, false);
        }

        private static Dictionary<string, RailNode> BuildRails(Transform root)
        {
            Transform group = GetGroup(root, PlazaLayout.RailsGroup);
            var nodes = new Dictionary<string, RailNode>();
            foreach(PlazaLayout.NodeEntry entry in PlazaLayout.Nodes)
            {
                GameObject go = CreateChild(group, $"RailNode_{entry.name}");
                go.transform.position = entry.position;
                nodes[entry.name] = go.AddComponent<RailNode>();
            }

            foreach(PlazaLayout.LinkEntry link in PlazaLayout.Links)
                nodes[link.a].ConnectTo(nodes[link.b]);

            int interactableLayer = LayerMask.NameToLayer(InteractableLayerName);
            foreach(PlazaLayout.ConnectorEntry entry in PlazaLayout.Connectors)
            {
                GameObject go = CreateChild(group, entry.name);
                go.transform.position = entry.position;
                go.layer = interactableLayer;

                var trigger = go.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = ConnectorTriggerRadius;

                var connector = go.AddComponent<RailConnector>();
                connector.SetDestinationNode(nodes[entry.destination]);
                SetProperties(connector, ("isWarp", false), ("promptText", entry.prompt));
            }
            return nodes;
        }

        private static void BuildNpcs(Transform root, Component dialogueCamera)
        {
            Transform group = GetGroup(root, PlazaLayout.NpcGroup);
            var npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlazaLayout.NpcPrefabPath);
            if(npcPrefab == null)
                Debug.LogWarning($"[PlazaSceneBuilder] {PlazaLayout.NpcPrefabPath} missing, talkable NPCs skipped");

            foreach(PlazaLayout.NpcEntry entry in PlazaLayout.Npcs)
            {
                if(!string.IsNullOrEmpty(entry.prefabPath))
                {
                    var entryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.prefabPath);
                    if(entryPrefab == null)
                    {
                        Debug.LogWarning($"[PlazaSceneBuilder] {entry.prefabPath} missing, {entry.name} skipped");
                        continue;
                    }

                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(entryPrefab, group);
                    instance.name = entry.name;
                    instance.transform.position = entry.position;
                    continue;
                }
                if(entry.dialogueId == 0)
                {
                    CreateSpriteObject(group, entry.name, entry.spritePath, entry.controllerPath, entry.position, true);
                    continue;
                }
                if(npcPrefab == null)
                    continue;

                var go = (GameObject)PrefabUtility.InstantiatePrefab(npcPrefab, group);
                go.name = $"NPC_{entry.name}";
                go.transform.position = entry.position;

                SetProperties(go.GetComponent<NPC>(),
                    ("objectID", entry.objectId),
                    ("dialogueId", entry.dialogueId),
                    ("characterId", entry.CharacterId),
                    ("dialogueCamera", dialogueCamera));

                var spriteRenderer = go.GetComponentInChildren<SpriteRenderer>(true);
                if(spriteRenderer != null)
                    SetupVisual(spriteRenderer, entry.name, entry.spritePath, entry.controllerPath);
            }
        }

        /// <summary>빈 루트 + 스프라이트 자식 V_{name}. 루트 위치가 스프라이트 아래쪽 끝(발).</summary>
        private static void CreateSpriteObject(Transform parent, string name, string spritePath, string controllerPath, Vector3 position, bool billboard)
        {
            GameObject go = CreateChild(parent, name);
            go.transform.position = position;

            GameObject visual = CreateChild(go.transform, $"V_{name}");
            var spriteRenderer = visual.AddComponent<SpriteRenderer>();
            spriteRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(PlazaLayout.SpriteMaterialPath);

            var npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlazaLayout.NpcPrefabPath);
            SpriteRenderer template = npcPrefab != null ? npcPrefab.GetComponentInChildren<SpriteRenderer>(true) : null;
            if(template != null)
                spriteRenderer.sortingLayerID = template.sortingLayerID;

            if(billboard)
                visual.AddComponent<BillboardHandler>();
            SetupVisual(spriteRenderer, name, spritePath, controllerPath);
        }

        // 스프라이트 피벗이 중앙이므로 자식을 -bounds.min.y 만큼 올려 발을 부모 위치에 맞춘다
        private static void SetupVisual(SpriteRenderer spriteRenderer, string name, string spritePath, string controllerPath)
        {
            spriteRenderer.gameObject.name = $"V_{name}";
            Sprite sprite = LoadSprite(spritePath);
            if(sprite == null)
                Debug.LogWarning($"[PlazaSceneBuilder] No sprite in {spritePath} ({name})");
            else
            {
                spriteRenderer.sprite = sprite;
                spriteRenderer.transform.localPosition = new Vector3(0f, -sprite.bounds.min.y, 0f);
            }

            if(!string.IsNullOrEmpty(controllerPath))
            {
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                if(controller == null)
                    Debug.LogWarning($"[PlazaSceneBuilder] Animator controller missing: {controllerPath}");
                else
                {
                    var animator = spriteRenderer.GetComponent<Animator>();
                    if(animator == null)
                        animator = spriteRenderer.gameObject.AddComponent<Animator>();
                    animator.runtimeAnimatorController = controller;
                }
            }

            SetProperties(spriteRenderer.GetComponent<BillboardHandler>(), ("lockYAxis", PlazaLayout.CharacterBillboardLockYAxis));
        }

        #endregion

        #region Particles (D6)

        private static void CreateSnow(Transform mainCamera)
        {
            GameObject go = CreateChild(mainCamera, SnowName);
            go.transform.localPosition = SnowLocalPosition;
            var particles = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = SnowLifetime;
            main.startSpeed = 0f;
            main.gravityModifier = SnowGravity;
            main.maxParticles = SnowMaxParticles;
            main.startSize = new ParticleSystem.MinMaxCurve(SnowSizeMin, SnowSizeMax);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = SnowRate;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = SnowBoxSize;

            // 세 축 커브 모드가 같아야 한다
            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-SnowDrift, SnowDrift);
            velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = SnowNoise;
            noise.frequency = SnowNoise;

            SetParticleMaterial(particles);
        }

        private static void CreateSmoke(Transform parent, PlazaLayout.ParticleEntry entry)
        {
            ParticleSystem particles = CreateUpwardEmitter(parent, entry, SmokeAngle, SmokeRadius);
            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = SmokeLifetime;
            main.startSpeed = SmokeSpeed;
            main.startSize = new ParticleSystem.MinMaxCurve(SmokeSizeMin, SmokeSizeMax);
            main.startColor = SmokeColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = SmokeRate;

            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = fade;
        }

        private static ParticleSystem CreateUpwardEmitter(Transform parent, PlazaLayout.ParticleEntry entry, float angle, float radius)
        {
            GameObject go = CreateChild(parent, entry.name);
            go.transform.position = entry.position;
            var particles = go.AddComponent<ParticleSystem>();

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle;
            shape.radius = radius;
            shape.rotation = EmitUp;

            SetParticleMaterial(particles);
            return particles;
        }

        private static void SetParticleMaterial(ParticleSystem particles)
        {
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetMaterial(SnowMaterialName, Color.white);
        }

        #endregion

        #region Checks

        // 보정 8: 레인 F 위 발 높이에서 카메라 쪽으로 쏜 광선이 앞쪽(레인 A, 광장, 분수) 오브젝트 bounds에 걸리면 경고
        private static void CheckLaneFOcclusion(Transform root)
        {
            Vector3 toCamera = Quaternion.Euler(ToolDefines.PlazaCameraPitch, 0f, 0f) * Vector3.back;
            var objects = new List<(string name, Bounds bounds)>();
            foreach(string group in new[] { PlazaLayout.LaneAGroup, PlazaLayout.SquareGroup, PlazaLayout.FountainGroup })
                CollectChildBounds(GetGroup(root, group), objects);

            var occluders = new HashSet<string>();
            for(float x = PlazaLayout.RampLX; x <= PlazaLayout.RampRX; x += OcclusionSampleStep)
            {
                var ray = new Ray(new Vector3(x, PlazaLayout.LaneFY + OcclusionFeetOffset, PlazaLayout.LaneFZ), toCamera);
                foreach((string name, Bounds bounds) in objects)
                {
                    if(bounds.IntersectRay(ray))
                        occluders.Add(name);
                }
            }

            if(occluders.Count > 0)
                Debug.LogWarning($"[PlazaSceneBuilder] Lane F is hidden behind: {string.Join(", ", occluders)}. Lower or move them in PlazaLayout");
        }

        // 보정 9: 경사로 콜라이더가 건물 footprint와 겹치면 플레이어가 건물을 통과해 걷는다
        private static void CheckRampFootprints(Transform root)
        {
            Physics.SyncTransforms();
            var objects = new List<(string name, Bounds bounds)>();
            CollectChildBounds(GetGroup(root, PlazaLayout.LaneAGroup), objects);
            CollectChildBounds(GetGroup(root, PlazaLayout.SquareGroup), objects);

            foreach(PlazaLayout.BoxEntry box in PlazaLayout.Boxes)
            {
                if(!box.walkable || !box.hidden)
                    continue;

                Transform ramp = GetGroup(root, box.group).Find(box.name);
                Collider rampCollider = ramp != null ? ramp.GetComponent<Collider>() : null;
                if(rampCollider == null)
                    continue;

                foreach((string name, Bounds bounds) in objects)
                {
                    if(rampCollider.bounds.Intersects(bounds))
                        Debug.LogWarning($"[PlazaSceneBuilder] {box.name} crosses {name}. Move one of them in PlazaLayout");
                }
            }
        }

        private static void CollectChildBounds(Transform group, List<(string name, Bounds bounds)> result)
        {
            foreach(Transform child in group)
            {
                if(TryGetBounds(child.gameObject, out Bounds bounds))
                    result.Add((child.name, bounds));
            }
        }

        #endregion

        #region Helpers

        private static Vector3 GetNodePosition(string name)
        {
            foreach(PlazaLayout.NodeEntry node in PlazaLayout.Nodes)
            {
                if(node.name == name)
                    return node.position;
            }
            return Vector3.zero;
        }

        private static bool TryGetBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if(renderers.Length == 0)
                return false;

            bounds = renderers[0].bounds;
            for(int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        private static Sprite LoadSprite(string path)
        {
            foreach(Object asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
            {
                if(asset is Sprite sprite)
                    return sprite;
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Art/Materials/{name}.mat. 없으면 MAT_Placeholder_* 단색 머티리얼을 만들어 쓴다.</summary>
        private static Material GetMaterial(string name, Color color)
        {
            if(string.IsNullOrEmpty(name))
                return null;

            var material = AssetDatabase.LoadAssetAtPath<Material>($"{ToolDefines.PlazaMaterialFolder}/{name}.mat");
            if(material != null)
                return material;

            if(!name.StartsWith(PlaceholderPrefix))
            {
                Debug.LogWarning($"[PlazaSceneBuilder] {name} missing (run 'LOTW/Plaza/2 Setup Render'), using a placeholder");
                name = PlaceholderPrefix + name.Substring(name.IndexOf('_') + 1);
                material = AssetDatabase.LoadAssetAtPath<Material>($"{ToolDefines.PlazaMaterialFolder}/{name}.mat");
                if(material != null)
                    return material;
            }

            Shader shader = Shader.Find(LitShaderName);
            if(shader == null)
            {
                Debug.LogError($"[PlazaSceneBuilder] Shader not found: {LitShaderName}");
                return null;
            }

            material = new Material(shader);
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, $"{ToolDefines.PlazaMaterialFolder}/{name}.mat");
            return material;
        }

        private static Type FindType(string assemblyQualifiedName)
        {
            Type type = Type.GetType(assemblyQualifiedName);
            if(type == null)
                Debug.LogError($"[PlazaSceneBuilder] Type not found: {assemblyQualifiedName}");
            return type;
        }

        private static Component AddComponent(GameObject go, string typeName)
        {
            Type type = FindType(typeName);
            if(type == null)
                return null;

            Component component = go.GetComponent(type);
            return component != null ? component : go.AddComponent(type);
        }

        private static void SetProperties(Object target, params (string path, object value)[] values)
        {
            if(target == null)
                return;

            var serialized = new SerializedObject(target);
            foreach((string path, object value) in values)
            {
                SerializedProperty property = serialized.FindProperty(path);
                if(property == null)
                {
                    Debug.LogWarning($"[PlazaSceneBuilder] {target.GetType().Name}.{path} not found");
                    continue;
                }

                switch(value)
                {
                    case null:
                        property.objectReferenceValue = null;
                        break;
                    case bool b:
                        property.boolValue = b;
                        break;
                    case int i:
                        property.intValue = i;
                        break;
                    case float f:
                        property.floatValue = f;
                        break;
                    case string s:
                        property.stringValue = s;
                        break;
                    case Vector2 v2:
                        property.vector2Value = v2;
                        break;
                    case Vector3 v3:
                        property.vector3Value = v3;
                        break;
                    case Object o:
                        property.objectReferenceValue = o;
                        break;
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion
    }
}
