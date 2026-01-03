using UnityEngine;
using Project.Scripts.Core.Managers;

namespace Project.Scripts.Core
{
    public static class Bootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Execute()
        {
            if (Object.FindFirstObjectByType<SystemRoot>() != null)
                return;

            Object resource = Resources.Load("SubSystemCollection");
            if (resource == null)
            {
                Debug.LogError("[Bootstrapper] Couldn't Find 'Resources/SubSystemCollection'.");
                return;
            }

            GameObject managers = Object.Instantiate(resource) as GameObject;
            
            if(managers != null)
            {
                managers.name = "[SubSystemCollection]";
                Object.DontDestroyOnLoad(managers);
            }
        }
    }
}