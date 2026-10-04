using UnityEngine;
using Project.Scripts.Core.Managers;

namespace Project.Scripts.System.World
{
    /// <summary>
    /// 씬 시작 시 BGM을 재생합니다.
    /// </summary>
    public class SceneBgmStarter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private AudioClip bgm;

        private void Start()
        {
            AudioManager.Instance.PlayBGM(bgm);
        }
    }
}
