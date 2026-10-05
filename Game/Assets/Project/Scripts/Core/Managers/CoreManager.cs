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

        #region Fields

        // 앱이 백그라운드로 가서(에디터에서는 포커스를 잃어서) 멈춘 경우만 true. 돌아오면 이 경우만 푼다.
        private bool _pausedByApplication;

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
            _pausedByApplication = false;
            Time.timeScale = 1f;
            OnGameResumed?.Invoke();
        }

        // 백그라운드로 가면 멈추고, 돌아오면 그때 멈춘 것만 푼다(플레이어가 연 일시정지는 유지).
        private void OnApplicationPause(bool pauseStatus)
        {
            if(pauseStatus)
            {
                if(IsPaused)
                    return;
                PauseGame();
                _pausedByApplication = true;
            }
            else if(_pausedByApplication)
            {
                ResumeGame();
            }
        }

        private void OnApplicationQuit()
        {
            Time.timeScale = 1f;
        }

        #endregion
    }
}
