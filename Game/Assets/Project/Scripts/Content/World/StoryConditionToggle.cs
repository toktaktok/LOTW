using UnityEngine;
using Project.Scripts.Content.Dialogue;
using Project.Scripts.Core.Managers;
using Project.Scripts.System.Dialogue;

namespace Project.Scripts.Content.World
{
    /// <summary>
    /// 플래그 조건에 따라 대상 오브젝트를 켜고 끕니다. 시간대별 조명/배경 소품, 진행에 따라 나타나는 주민 등.
    /// 예: 밤에만 켜지는 가로등 'flag:timeSlot==3', 의뢰 완료 후 사라지는 NPC '!quest:101=2'.
    /// 대상은 이 컴포넌트가 붙은 오브젝트 밖이나 자식으로 둡니다 (자기 자신을 끄면 다시 켤 수 없음).
    /// </summary>
    public class StoryConditionToggle : MonoBehaviour
    {
        [Tooltip("대화 조건 문법. 비어 있으면 항상 켜짐")]
        [SerializeField] private string conditions;
        [SerializeField] private GameObject[] targets;
        [Tooltip("켜면 조건을 만족할 때 끕니다")]
        [SerializeField] private bool invert;

        private readonly IDialogueContext _context = new ManagerDialogueContext();
        private bool _dirty;

        private void OnEnable()
        {
            FlagManager.OnFlagChanged += OnFlagChanged;
            Apply();
        }

        private void OnDisable()
        {
            FlagManager.OnFlagChanged -= OnFlagChanged;
        }

        // 한 액션 줄에서 플래그가 여러 개 바뀌어도 프레임당 한 번만 평가
        private void LateUpdate()
        {
            if(_dirty)
                Apply();
        }

        private void OnFlagChanged(string key)
        {
            _dirty = true;
        }

        private void Apply()
        {
            _dirty = false;
            bool active = DialogueCommands.CheckConditions(conditions, _context) != invert;
            foreach(GameObject target in targets)
            {
                if(target != null && target != gameObject && target.activeSelf != active)
                    target.SetActive(active);
            }
        }
    }
}
