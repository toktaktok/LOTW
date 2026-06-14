using System;
using System.IO;
using UnityEngine;
using Project.Scripts.Data.Table;

namespace Project.Scripts.System.World.Map
{
    /// <summary>
    /// 맵 저장/로드. SaveManager 관용구(JsonUtility.ToJson + File.WriteAllText to persistentDataPath)를 그대로 따릅니다.
    /// UnityEditor를 참조하지 않으므로 빌드에서도 동작하고, 저장은 Play Mode 중에 완료되어 종료 시점 경계에 의존하지 않습니다.
    /// </summary>
    public static class MapSerializer
    {
        public const string MapFolder = "Maps";

        public static string LastSavedPath { get; private set; }
        public static bool DirtySaved { get; private set; }

        /// <summary>에디터(Project.Scripts.Editor)가 구독합니다. Play Mode 종료 시 기본 맵으로 승격.</summary>
        public static event Action<string> OnPromoteRequested;

        public static string ResolveMapPath(string mapName)
        {
            return Path.Combine(Application.persistentDataPath, MapFolder, $"map_{mapName}.json");
        }

        public static string Save(MapData data, string mapName)
        {
            string dir = Path.Combine(Application.persistentDataPath, MapFolder);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, $"map_{mapName}.json");
            File.WriteAllText(path, JsonUtility.ToJson(data, true));

            LastSavedPath = path;
            DirtySaved = true;
            return path;
        }

        public static MapData Load(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;
            try
            {
                return JsonUtility.FromJson<MapData>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MapSerializer] Failed to load map at {path}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 단일 MapData 객체 JSON을 JsonArrayHelper가 읽는 1개짜리 raw 배열 [ {..} ] 로 감쌉니다.
        /// 객체 vs 배열 비대칭은 오직 이 한 곳에만 존재합니다.
        /// </summary>
        public static string WrapAsDefaultArray(string singleObjectJson)
        {
            return "[" + singleObjectJson + "]";
        }

        public static void RequestPromote()
        {
            if (!DirtySaved || string.IsNullOrEmpty(LastSavedPath))
            {
                Debug.LogWarning("[MapSerializer] Promote ignored: no map saved this session.");
                return;
            }
            OnPromoteRequested?.Invoke(LastSavedPath);
        }
    }
}
