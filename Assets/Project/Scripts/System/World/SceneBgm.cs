using UnityEngine;
using Project.Scripts.Core.Managers;

namespace Project.Scripts.System.World
{
    /// <summary>
    /// 씬 시작 시 BGM을 재생합니다. 씬마다 하나 배치하고 클립만 지정합니다.
    /// 같은 클립이 이미 재생 중이면 끊지 않고 이어지며, 다른 클립이면 크로스페이드합니다.
    /// </summary>
    public class SceneBgm : MonoBehaviour
    {
        [SerializeField] private AudioClip bgm;
        [SerializeField] private bool crossfade = true;

        private void Start()
        {
            if(bgm == null)
            {
                Debug.LogWarning($"[SceneBgm] {gameObject.scene.name}: BGM 클립이 지정되지 않았습니다.");
                return;
            }

            AudioManager.Instance.PlayBGM(bgm, crossfade);
        }
    }
}
