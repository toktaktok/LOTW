using System;
using UnityEngine;

namespace Project.Scripts.Core.Managers
{
    public class CoreManager : Singleton<CoreManager>
    {
        #region Events

        public static event Action OnGamePaused;
        public static event Action OnGameResumed;

        #endregion

        #region Properties

        public bool IsPaused { get; private set; }

        #endregion

        #region Methods

        public void PauseGame()
        {
            if(IsPaused)
                return;

            IsPaused = true;
            Time.timeScale = 0f;
            OnGamePaused?.Invoke();
        }

        public void ResumeGame()
        {
            if(!IsPaused)
                return;

            IsPaused = false;
            Time.timeScale = 1f;
            OnGameResumed?.Invoke();
        }

        // 백그라운드로 가면 멈추되, 돌아올 때 자동으로 풀지 않는다(플레이어가 연 일시정지 유지).
        private void OnApplicationPause(bool pauseStatus)
        {
            if(pauseStatus)
                PauseGame();
        }

        private void OnApplicationQuit()
        {
            Time.timeScale = 1f;
        }

        #endregion
    }
}
