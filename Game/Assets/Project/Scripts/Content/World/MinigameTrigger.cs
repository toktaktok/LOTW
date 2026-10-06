using UnityEngine;
using Project.Scripts.Content.UI;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;
using Project.Scripts.System.World;
using Project.Scripts.Framework;
using Project.Scripts.Framework.Managers;

namespace Project.Scripts.Content.World
{
    /// <summary>
    /// 상호작용하면 미니게임을 여는 월드 오브젝트. Interactable 레이어와 트리거 콜라이더가 필요합니다.
    /// 시작 조건을 만족하지 않으면 정의의 deniedDialogueId 대화를 열고, 그것도 없으면 상호작용 대상에서 빠집니다.
    /// </summary>
    public class MinigameTrigger : WorldObject, IInteractable
    {
        [Header("Minigame")]
        [SerializeField] private MinigameDefinition definition;

        [Header("Interaction")]
        [Tooltip("'@키'로 Text 테이블 참조 가능")]
        [SerializeField] private string promptText = "살펴보기";

        public string InteractionPrompt => promptText;

        public bool CanInteract
        {
            get
            {
                if(definition == null)
                    return false;
                return definition.DeniedDialogueId >= 0 || MinigameManager.Instance.CanStart(definition);
            }
        }

        public void Interact(GameObject interactor)
        {
            if(definition == null || MinigameManager.Instance.Play(definition, gameObject))
                return;
            if(definition.DeniedDialogueId < 0)
                return;

            DialogueData denied = DataManager.Instance.GetRow<DialogueData>(definition.DeniedDialogueId);
            if(denied != null)
                UIManager.Instance.PushPage<DialogueUI>(UILayer.Popup, ui => ui.SetupDialogue(denied, this, interactor));
        }
    }
}
