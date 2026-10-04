using UnityEngine;

namespace Project.Scripts.Data
{
    /// <summary>
    /// 아이템 데이터를 담는 ScriptableObject.
    /// Resources/Items/ 폴더에 itemId와 동일한 이름으로 저장합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "LOTW/Item Data")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private ItemData data;

        public ItemData Data => data;
    }
}
