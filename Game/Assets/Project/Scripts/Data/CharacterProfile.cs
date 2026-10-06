using UnityEngine;

namespace Project.Scripts.Data
{
    /// <summary>
    /// 캐릭터 외형/이름 데이터. PF_NPC_Base 인스턴스에 할당하면 스프라이트와 애니메이터가 적용됩니다.
    /// 에셋 위치: Data/Characters/CharacterProfile_{Name}.asset
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterProfile_New", menuName = "LOTW/Character Profile")]
    public class CharacterProfile : ScriptableObject
    {
        [Tooltip("표시 이름. Text 키 (@키). 원문은 TextKeyReferenceTests 가 막음")]
        [SerializeField] private string displayName;
        [SerializeField] private Sprite sprite;
        [Tooltip("비워두면 정지 스프라이트만 사용")]
        [SerializeField] private RuntimeAnimatorController animatorController;

        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
        public RuntimeAnimatorController AnimatorController => animatorController;
    }
}
