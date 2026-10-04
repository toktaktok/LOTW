#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using Project.Scripts.System.World.Map;

namespace Project.Scripts.Editor.Map
{
    /// <summary>
    /// (선택) 플레이 모드에서 저장한 유저 맵을 체크인되는 기본 맵(Resources/Table/Map.json)으로 승격합니다.
    /// 코어 기능과 무관한 편의 도구이며, 에디터 전용(AssetDatabase/EditorApplication/SessionState)이라 빌드에 포함되지 않습니다.
    /// 런타임은 P 키로 MapSerializer.OnPromoteRequested를 발행만 하고, 실제 복사는 플레이 종료 시 여기서 처리합니다.
    /// </summary>
    [InitializeOnLoad]
    public static class MapDefaultPromoter
    {
        private const string KeyRequested = "LOTW.Map.PromoteRequested";
        private const string KeyPath = "LOTW.Map.LastSavedPath";
        private const string DefaultAssetPath = "Assets/Project/Resources/Table/Map.json";

        static MapDefaultPromoter()
        {
            MapSerializer.OnPromoteRequested += OnPromoteRequested;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPromoteRequested(string savedPath)
        {
            // 도메인 리로드를 견디도록 SessionState에 의도를 기록하고, 실제 적용은 종료 시점으로 미룬다.
            SessionState.SetBool(KeyRequested, true);
            SessionState.SetString(KeyPath, savedPath);
            Debug.Log("[MapDefaultPromoter] Promote requested; will apply on play-mode exit.");
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode)
                return;
            if (!SessionState.GetBool(KeyRequested, false))
                return;

            string savedPath = SessionState.GetString(KeyPath, "");
            SessionState.EraseBool(KeyRequested);
            SessionState.EraseString(KeyPath);

            if (string.IsNullOrEmpty(savedPath) || !File.Exists(savedPath))
            {
                Debug.LogWarning("[MapDefaultPromoter] No saved map file to promote.");
                return;
            }

            string singleObject = File.ReadAllText(savedPath);
            File.WriteAllText(DefaultAssetPath, MapSerializer.WrapAsDefaultArray(singleObject));
            AssetDatabase.Refresh();
            Debug.Log($"[MapDefaultPromoter] Promoted {savedPath} -> {DefaultAssetPath}");
        }
    }
}
#endif
