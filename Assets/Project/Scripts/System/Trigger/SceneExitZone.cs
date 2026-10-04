using UnityEngine;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.System.World;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project.Scripts.System.Trigger
{
    /// <summary>
    /// 씬 전환을 트리거하는 인터랙터블 오브젝트입니다.
    /// PlayerInteractor가 상호작용하면 지정된 씬으로 전환합니다.
    /// </summary>
    public class SceneExitZone : MonoBehaviour, IInteractable
    {
        [Header("Destination")]
        [Tooltip("전환할 씬 이름 (SceneDefines 참고)")]
        [SerializeField] private string targetScene;

        [Tooltip("도착 씬의 SceneEntrance ID")]
        [SerializeField] private string targetEntranceId;

        [Header("Interaction")]
        [SerializeField] private string promptText = "이동하기";

        public string InteractionPrompt => promptText;

        public void Interact(GameObject interactor)
        {
            if(string.IsNullOrEmpty(targetScene))
            {
                Debug.LogWarning($"[SceneExitZone] {name}: targetScene이 비어 있습니다.");
                return;
            }

            SceneTransitionManager.Instance.TransitionTo(targetScene, targetEntranceId);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
            Gizmos.DrawCube(transform.position, Vector3.one * 0.8f);

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.8f);
            Gizmos.DrawWireSphere(transform.position, WorldDefines.InteractionDistance);

            string label = string.IsNullOrEmpty(targetScene)
                ? "[Exit] (씬 없음)"
                : $"[Exit] → {targetScene}\n [{targetEntranceId}]";
            Handles.Label(transform.position + Vector3.up * 1.2f, label);
        }
#endif
    }
}
