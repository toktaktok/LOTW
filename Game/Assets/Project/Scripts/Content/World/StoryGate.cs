using UnityEngine;
using Project.Scripts.Content.Dialogue;
using Project.Scripts.Content.UI;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data.Table;
using Project.Scripts.System.Dialogue;
using Project.Scripts.System.World;
using Project.Scripts.Framework;
using Project.Scripts.Framework.Managers;

namespace Project.Scripts.Content.World
{
    /// <summary>
    /// 지역 게이트. 대화 조건 문법의 conditions 를 만족해야 같은 오브젝트의 RailConnector / SceneExitZone 이 이동합니다.
    /// 예: 광장 핵심 주민 수첩 정보가 차야 다음 지역으로 ('note:102;note:103;met:npc_mayor').
    /// 막히면 lockedDialogueId 내레이션을 엽니다.
    /// </summary>
    public class StoryGate : MonoBehaviour, IInteractionGate
    {
        [Tooltip("대화 조건 문법. 비어 있으면 항상 통과")]
        [SerializeField] private string conditions;
        [Tooltip("막혔을 때 여는 Dialogue 테이블 DataID (0 이면 아무것도 표시하지 않음)")]
        [SerializeField] private int lockedDialogueId;

        public bool TryPass(GameObject interactor)
        {
            if(DialogueCommands.CheckConditions(conditions, new ManagerDialogueContext()))
                return true;

            DialogueData locked = lockedDialogueId != 0 ? DataManager.Instance.GetRow<DialogueData>(lockedDialogueId) : null;
            if(locked != null)
                UIManager.Instance.PushPage<DialogueUI>(UILayer.Popup, ui => ui.SetupDialogue(locked, null, interactor));
            return false;
        }
    }
}
