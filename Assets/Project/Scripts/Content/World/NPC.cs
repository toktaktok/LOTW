using UnityEngine;
using Project.Scripts.Content.UI;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;
using Project.Scripts.System.World;

namespace Project.Scripts.Content.World
{
    /// <summary>
    /// 대화 가능한 NPC. IInteractable을 구현하여 PlayerInteractor와 연동됩니다.
    /// 상호작용하면 Dialogue 테이블의 시작 행부터 DialogueUI 대화 모드를 엽니다.
    /// </summary>
    public class NPC : WorldObject, IInteractable
    {
        [Header("NPC Settings")]
        [SerializeField] private string npcName = "NPC";

        [Header("Dialogue")]
        [Tooltip("Dialogue 테이블의 시작 DataID")]
        [SerializeField] private int dialogueId;

        [Header("Interaction")]
        [SerializeField] private string promptText = "대화하기";

        public string InteractionPrompt => promptText;

        public void Interact(GameObject interactor)
        {
            DialogueData start = DataManager.Instance.GetRow<DialogueData>(dialogueId);
            if(start == null)
            {
                Debug.LogWarning($"[NPC] {npcName}: Dialogue 테이블에 dataId {dialogueId} 행이 없습니다.");
                return;
            }

            UIManager.Instance.PushPage<DialogueUI>(UILayer.Popup,
                ui => ui.SetupDialogue(start, this, interactor));
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.5f);
            Gizmos.DrawSphere(transform.position + Vector3.up * 1.5f, 0.2f);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"[NPC] {npcName}");
        }
#endif
    }
}
