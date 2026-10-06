using UnityEngine;
using Project.Scripts.Framework.Managers;

namespace Project.Scripts.Framework
{
    /// <summary>
    /// 씬 로드 전에 매니저 프리팹을 자동 인스턴스화합니다.
    /// 프리팹 경로는 BootstrapConfig 에셋 또는 기본값("SubSystemCollection")을 사용합니다.
    /// </summary>
    public static class Bootstrapper
    {
        private const string DefaultPrefabPath = "SubSystemCollection";
        private const string ConfigPath = "BootstrapConfig";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Execute()
        {
            if(Object.FindFirstObjectByType<SystemRoot>() != null)
                return;

            string prefabPath = LoadPrefabPath();
            Object resource = Resources.Load(prefabPath);

            if(resource == null)
            {
                Debug.LogError($"[Bootstrapper] Couldn't find 'Resources/{prefabPath}'.");
                return;
            }

            GameObject managers = Object.Instantiate(resource) as GameObject;

            if(managers != null)
            {
                managers.name = "[SubSystemCollection]";
                Object.DontDestroyOnLoad(managers);
            }
        }

        private static string LoadPrefabPath()
        {
            var config = Resources.Load<BootstrapConfig>(ConfigPath);
            if(config != null && !string.IsNullOrEmpty(config.PrefabPath))
                return config.PrefabPath;

            return DefaultPrefabPath;
        }
    }

    /// <summary>
    /// Bootstrapper가 로드할 매니저 프리팹 경로를 지정하는 설정 에셋.
    /// Resources/BootstrapConfig에 배치합니다.
    /// 없으면 기본값("SubSystemCollection")을 사용합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "BootstrapConfig", menuName = "LOTW/Bootstrap Config")]
    public class BootstrapConfig : ScriptableObject
    {
        [Tooltip("Resources/ 기준 매니저 프리팹 경로 (확장자 제외)")]
        [SerializeField] private string prefabPath = "SubSystemCollection";

        public string PrefabPath => prefabPath;
    }
}
