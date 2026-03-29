using UnityEngine;
using Project.Scripts.Content.UI;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.System.World;

namespace Project.Scripts.Content.World
{
    /// <summary>
    /// 대화 가능한 NPC. IInteractable을 구현하여 PlayerInteractor와 연동됩니다.
    /// 상호작용하면 DialogueUI를 다단계 대화 모드로 엽니다.
    /// </summary>
    public class NPC : WorldObject, IInteractable
    {
        [Header("NPC Settings")]
        [SerializeField] private string npcName = "NPC";

        [Header("Dialogue")]
        [SerializeField] private DialogueData dialogue;

        [Header("Interaction")]
        [SerializeField] private string promptText = "대화하기";

        public string InteractionPrompt => promptText;

        public void Interact(GameObject interactor)
        {
            if (dialogue.lines == null || dialogue.lines.Length == 0)
            {
                Debug.LogWarning($"[NPC] {npcName}: 대화 데이터가 비어 있습니다.");
                return;
            }

            UIManager.Instance.PushPage<DialogueUI>(UILayer.Popup,
                ui => ui.SetupDialogue(dialogue, this, interactor));
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
