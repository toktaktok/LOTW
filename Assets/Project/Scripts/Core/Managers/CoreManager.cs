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
            if (IsPaused) return;

            IsPaused = true;
            Time.timeScale = 0f;
            OnGamePaused?.Invoke();
        }

        public void ResumeGame()
        {
            if (!IsPaused) return;

            IsPaused = false;
            Time.timeScale = 1f;
            OnGameResumed?.Invoke();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                PauseGame();
            else
                ResumeGame();
        }

        private void OnApplicationQuit()
        {
            Time.timeScale = 1f;
        }

        #endregion
    }
}
