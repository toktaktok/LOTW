namespace Project.Scripts.Data.Table
{
    /// <summary>
    /// 아이템 테이블 한 행. 런타임 표현(Sprite 포함)은 Data.ItemData 이며 TableItemDataProvider 가 변환합니다.
    /// Excel: Table/Excel/Item.xlsx  (DataId, ItemId, DisplayName, Description, Icon, MaxStack)
    /// icon 은 Resources/Items/{icon} 스프라이트 파일 이름입니다. displayName, description 은 Text 키(@키).
    /// 필드는 Generated/ItemTableData.cs.
    /// </summary>
    public partial class ItemTableData
    {
    }
}
