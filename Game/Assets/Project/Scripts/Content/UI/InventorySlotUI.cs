using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project.Scripts.Data;

namespace Project.Scripts.Content.UI
{
    /// <summary>
    /// 인벤토리 슬롯 하나의 표시를 담당합니다.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject emptyOverlay;

        public void Set(ItemSlot slot, ItemData? data)
        {
            if(slot.IsEmpty)
            {
                SetEmpty();
                return;
            }

            if(iconImage != null)
            {
                iconImage.sprite = data?.icon;
                iconImage.enabled = data?.icon != null;
            }

            if(countText != null)
            {
                countText.text = slot.count > 1 ? slot.count.ToString() : string.Empty;
                countText.enabled = true;
            }

            if(emptyOverlay != null)
                emptyOverlay.SetActive(false);
        }

        public void SetEmpty()
        {
            if(iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if(countText != null)
            {
                countText.text = string.Empty;
                countText.enabled = false;
            }

            if(emptyOverlay != null)
                emptyOverlay.SetActive(true);
        }
    }
}
