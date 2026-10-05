using UnityEngine;
using Project.Scripts.Content.Controller;
using Project.Scripts.Content.Dialogue;
using Project.Scripts.System.Dialogue;

namespace Project.Scripts.Content.Story
{
    /// <summary>
    /// 씬 시작 시 또는 플레이어가 트리거 콜라이더에 들어올 때 시퀀스를 재생합니다.
    /// 한 번만 재생하려면 시퀀스 마지막 actions 에서 플래그를 세우고 conditions 에 '!flag:그플래그' 를 넣습니다.
    /// </summary>
    public class SequenceTrigger : MonoBehaviour
    {
        [Tooltip("Sequence 테이블 시작 스텝 dataId")]
        [SerializeField] private int sequenceId;
        [Tooltip("대화 조건 문법. 비어 있으면 항상 재생")]
        [SerializeField] private string conditions;
        [Tooltip("켜면 씬 시작 시 재생. 끄면 트리거 콜라이더 진입 시 재생")]
        [SerializeField] private bool playOnStart;

        private void Start()
        {
            if(playOnStart)
                TryPlay();
        }

        private void OnTriggerEnter(Collider other)
        {
            if(!playOnStart && other.GetComponentInParent<PlayerController>() != null)
                TryPlay();
        }

        private void TryPlay()
        {
            if(SequencePlayer.IsPlaying || !DialogueCommands.CheckConditions(conditions, new ManagerDialogueContext()))
                return;

            SequencePlayer.Instance.Play(sequenceId);
        }
    }
}
