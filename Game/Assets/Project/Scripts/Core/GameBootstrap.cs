using UnityEngine;
using Project.Scripts.Framework.Managers;
using Project.Scripts.Content.World;

namespace Project.Scripts.Core
{
    /// <summary>
    /// 프레임워크 매니저에 LOTW 구현을 주입합니다. Bootstrapper 가 매니저 프리팩을 만든 뒤, 첫 Start 전에 실행됩니다.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Execute()
        {
            AudioManager.Instance.SetVolumeProvider(GameInstance.Instance);
            SceneTransitionManager.Instance.SetHandler(new GameSceneTransitionHandler());
        }
    }
}
