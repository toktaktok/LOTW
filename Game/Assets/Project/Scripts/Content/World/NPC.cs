using UnityEngine;
using Unity.Cinemachine;
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
        [Tooltip("대화 테이블 시작 행 dataId. 0이면 아래 인라인/SO 대화 사용")]
        [SerializeField] private int dialogueId;
        [SerializeField] private DialogueDatabase dialogueAsset;
        [SerializeField] private DialogueData dialogue;

        [Header("Camera")]
        [Tooltip("대화 중 전환할 카메라 (선택)")]
        [SerializeField] private CinemachineCamera dialogueCamera;

        [Header("Interaction")]
        [SerializeField] private string promptText = "대화하기";

        public string InteractionPrompt => promptText;

        private DialogueData ResolvedDialogue =>
            dialogueAsset != null ? dialogueAsset.Data : dialogue;

        public void Interact(GameObject interactor)
        {
            if(dialogueId != 0)
            {
                OpenTableDialogue(interactor);
                return;
            }

            var resolved = ResolvedDialogue;
            if(resolved.lines == null || resolved.lines.Length == 0)
            {
                Debug.LogWarning($"[NPC] {npcName}: 대화 데이터가 비어 있습니다.");
                return;
            }

            UIManager.Instance.PushPage<DialogueUI>(UILayer.Popup,
                ui => ui.SetupDialogue(resolved, this, interactor));
        }

        private void OpenTableDialogue(GameObject interactor)
        {
            CinemachineCamera previousCamera = CameraManager.Instance.CurrentCamera;
            // LowResPixelRenderer가 orthographicSize를 정수 배율로 스냅하므로 줌은 블렌드 중 한 번 툭 바뀜(의도된 동작)
            if(dialogueCamera != null)
                CameraManager.Instance.SwitchCamera(dialogueCamera, CameraDefines.DialogueBlendDuration);

            UIManager.Instance.PushPage<DialogueUI>(UILayer.Popup,
                ui => ui.SetupTableDialogue(dialogueId, this, interactor, () => RestoreCamera(previousCamera)));
        }

        private void RestoreCamera(CinemachineCamera previousCamera)
        {
            if(dialogueCamera == null || previousCamera == null)
                return;

            CameraManager.Instance.SwitchCamera(previousCamera, CameraDefines.DialogueBlendDuration);
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
