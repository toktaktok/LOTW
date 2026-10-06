using UnityEngine;
using Project.Scripts.Content.Story;
using Project.Scripts.Content.UI;
using Project.Scripts.Core.Managers;
using Project.Scripts.Framework;
using Project.Scripts.Framework.Managers;

namespace Project.Scripts.Content.Title
{
    /// <summary>
    /// Title 씬 시작점. 플레이 시간을 멈추고 메인 화면 UI 를 엽니다.
    /// (기획: 마지막 저장 장소에 따라 배경 연출이 바뀜 -> 배경 오브젝트가 생기면 여기서 고름)
    /// </summary>
    public class TitleScene : MonoBehaviour
    {
        [SerializeField] private AudioClip bgm;

        private void Start()
        {
            SequencePlayer.Instance.Stop();
            SaveManager.Instance.SetPlayTimeRunning(false);
            if(bgm != null)
                AudioManager.Instance.PlayBGM(bgm);
            UIManager.Instance.PushPage<TitleUI>(UILayer.Popup);
        }
    }
}
