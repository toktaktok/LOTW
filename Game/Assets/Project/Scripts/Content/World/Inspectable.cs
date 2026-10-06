using UnityEngine;
using Project.Scripts.Content.UI;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data.Table;
using Project.Scripts.System.World;
using Project.Scripts.Framework;
using Project.Scripts.Framework.Managers;

namespace Project.Scripts.Content.World
{
    /// <summary>
    /// 조사할 수 있는 물건 (책상 서류, 팸플릿, 포스터). 상호작용하면 Dialogue 테이블 행부터 내레이션 대화를 엽니다.
    /// 수첩 기록은 그 대화 행의 actions(addNote:id), 저장 지점은 actions(save)로 처리합니다.
    /// 콜라이더가 있어야 PlayerInteractor 가 찾습니다.
    /// </summary>
    public class Inspectable : MonoBehaviour, IInteractable
    {
        [Tooltip("Dialogue 테이블의 시작 DataID (분기 행 가능)")]
        [SerializeField] private int dialogueId;
        [Tooltip("'@키'로 Text 테이블 참조 가능")]
        [SerializeField] private string promptText = "@ui.inspect";

        public string InteractionPrompt => promptText;

        public void Interact(GameObject interactor)
        {
            DialogueData start = DataManager.Instance.GetRow<DialogueData>(dialogueId);
            if(start == null)
            {
                Debug.LogWarning($"[Inspectable] {name}: Dialogue 테이블에 dataId {dialogueId} 행이 없습니다.");
                return;
            }

            UIManager.Instance.PushPage<DialogueUI>(UILayer.Popup, ui => ui.SetupDialogue(start, this, interactor));
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.5f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
#endif
    }
}
