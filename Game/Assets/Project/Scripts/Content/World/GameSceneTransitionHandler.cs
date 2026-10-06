using UnityEngine;
using Project.Scripts.Core.Managers;
using Project.Scripts.Framework.Managers;
using Project.Scripts.System.World;
using Project.Scripts.Content.Controller;

namespace Project.Scripts.Content.World
{
    /// <summary>
    /// LOTW 씬 전환 핸들러: PlayerController를 SceneEntrance 위치에 배치하고 카메라 입력을 제어합니다.
    /// </summary>
    public class GameSceneTransitionHandler : ISceneTransitionHandler
    {
        public void OnBeforeTransition()
        {
            CameraManager.Instance.SetInput(false);
            // UI는 DontDestroyOnLoad라 이전 씬 대상을 붙든 페이지가 남음. HUD는 새 씬 PlayerController가 다시 연다.
            UIManager.Instance.ClearAllPages();
        }

        public void OnSceneLoaded(string sceneName, string entranceId)
        {
            if(string.IsNullOrEmpty(entranceId))
                return;

            SceneEntrance[] entrances = Object.FindObjectsByType<SceneEntrance>(FindObjectsSortMode.None);
            foreach(SceneEntrance entrance in entrances)
            {
                if(entrance.EntranceId != entranceId)
                    continue;

                PlayerController player = Object.FindFirstObjectByType<PlayerController>();
                if(player != null)
                    player.WarpToEntrance(entrance.SpawnPosition, entrance.StartNode);
                break;
            }
        }

        public void OnAfterTransition()
        {
            CameraManager.Instance.SetInput(true);
        }
    }
}
