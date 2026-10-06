using UnityEngine;
using Unity.Cinemachine;
using Project.Scripts.Content.UI;
using Project.Scripts.Core;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.Data.Table;
using Project.Scripts.System.World;
using Project.Scripts.Framework;
using Project.Scripts.Framework.Managers;

namespace Project.Scripts.Content.World
{
    /// <summary>
    /// 대화 가능한 NPC. IInteractable을 구현하여 PlayerInteractor와 연동됩니다.
    /// 상호작용하면 Dialogue 테이블의 시작 행부터 DialogueUI 대화 모드를 엽니다.
    /// 새 NPC는 PF_NPC_Base를 배치하고 profile(외형/이름)과 dialogueId만 지정합니다.
    /// </summary>
    public class NPC : WorldObject, IInteractable
    {
        [Header("NPC Settings")]
        [SerializeField] private CharacterProfile profile;
        [Tooltip("profile 스프라이트를 적용할 렌더러")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("대화/의뢰 테이블이 가리키는 ID (npc_gumman). 대화 횟수 플래그 talk.{id}, 의뢰 giverId 와 같아야 함")]
        [SerializeField] private string characterId;

        [Header("Dialogue")]
        [Tooltip("Dialogue 테이블의 시작 DataID (분기 행 가능)")]
        [SerializeField] private int dialogueId;

        [Header("Camera")]
        [Tooltip("대화 중 전환할 카메라 (선택)")]
        [SerializeField] private CinemachineCamera dialogueCamera;

        [Header("Interaction")]
        [Tooltip("Text 키 (@키). 원문은 TextKeyReferenceTests 가 막음")]
        [SerializeField] private string promptText = "@ui.talk";

        public string InteractionPrompt => promptText;
        public string DisplayName => profile != null ? Localization.Resolve(profile.DisplayName) : name;
        public string CharacterId => characterId ?? string.Empty;

        protected override void Awake()
        {
            base.Awake();
            ApplyProfile();
        }

        public void Interact(GameObject interactor)
        {
            DialogueData start = DataManager.Instance.GetRow<DialogueData>(dialogueId);
            if(start == null)
            {
                Debug.LogWarning($"[NPC] {DisplayName}: Dialogue 테이블에 dataId {dialogueId} 행이 없습니다.");
                return;
            }

            // 분기 행이 talk.{id} 로 횟수별 한마디를 고를 수 있도록 대화를 열기 전에 올림
            if(CharacterId.Length > 0)
                FlagManager.Instance.Add(StoryKeys.Talk(CharacterId));

            CinemachineCamera previousCamera = CameraManager.Instance.CurrentCamera;
            // LowResPixelRenderer가 orthographicSize를 정수 배율로 스냅하므로 줌은 블렌드 중 한 번 툭 바뀜(의도된 동작)
            if(dialogueCamera != null)
                CameraManager.Instance.SwitchCamera(dialogueCamera, CameraDefines.DialogueBlendDuration);

            UIManager.Instance.PushPage<DialogueUI>(UILayer.Popup,
                ui => ui.SetupDialogue(start, this, interactor, () => RestoreCamera(previousCamera)));
        }

        private void RestoreCamera(CinemachineCamera previousCamera)
        {
            if(dialogueCamera == null || previousCamera == null)
                return;

            CameraManager.Instance.SwitchCamera(previousCamera, CameraDefines.DialogueBlendDuration);
        }

        private void ApplyProfile()
        {
            if(profile == null || spriteRenderer == null)
                return;

            if(profile.Sprite != null)
                spriteRenderer.sprite = profile.Sprite;

            if(profile.AnimatorController == null)
                return;

            if(!spriteRenderer.TryGetComponent(out Animator animator))
                animator = spriteRenderer.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = profile.AnimatorController;
        }

#if UNITY_EDITOR
        // 씬에서 profile을 바꾸면 바로 스프라이트가 보이도록 미리보기
        private void OnValidate()
        {
            if(profile != null && profile.Sprite != null && spriteRenderer != null && spriteRenderer.sprite != profile.Sprite)
                spriteRenderer.sprite = profile.Sprite;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.5f);
            Gizmos.DrawSphere(transform.position + Vector3.up * 1.5f, 0.2f);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"[NPC] {(profile != null ? profile.DisplayName : name)}");
        }
#endif
    }
}
