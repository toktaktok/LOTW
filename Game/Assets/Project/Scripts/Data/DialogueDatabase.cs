using UnityEngine;

namespace Project.Scripts.Data
{
    /// <summary>
    /// 대화 데이터를 에셋으로 관리할 수 있는 ScriptableObject.
    /// NPC의 DialogueData 필드에 직접 할당하거나, 코드에서 로드하여 사용합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "LOTW/Dialogue Data")]
    public class DialogueDatabase : ScriptableObject
    {
        [SerializeField] private DialogueData data;

        public DialogueData Data => data;
    }
}
