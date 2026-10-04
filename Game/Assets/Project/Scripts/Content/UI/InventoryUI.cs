using UnityEngine;
using UnityEngine.UI;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.System.UI;

namespace Project.Scripts.Content.UI
{
    /// <summary>
    /// 인벤토리 화면 UI.
    /// InventoryManager의 슬롯 데이터를 표시합니다.
    /// </summary>
    public class InventoryUI : BaseUI
    {
        [Header("Inventory")]
        [SerializeField] private Transform slotContainer;
        [SerializeField] private Button closeButton;

        private InventorySlotUI[] _slotUIs;

        protected override void Awake()
        {
            base.Awake();
            closeButton?.onClick.AddListener(OnClose);
            CacheSlotUIs();
        }

        private void OnEnable()
        {
            InventoryManager.OnInventoryChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            InventoryManager.OnInventoryChanged -= Refresh;
        }

        private void CacheSlotUIs()
        {
            if(slotContainer != null)
                _slotUIs = slotContainer.GetComponentsInChildren<InventorySlotUI>(true);
        }

        public void Refresh()
        {
            if(_slotUIs == null || _slotUIs.Length == 0)
                return;

            var slots = InventoryManager.Instance.Slots;

            for(int i = 0; i < _slotUIs.Length; i++)
            {
                if(i < slots.Count)
                {
                    var slot = slots[i];
                    ItemData? data = slot.IsEmpty ? null : InventoryManager.Instance.GetItemData(slot.itemId);
                    _slotUIs[i].Set(slot, data);
                }
                else
                {
                    _slotUIs[i].SetEmpty();
                }
            }
        }

        private void OnClose()
        {
            UIManager.Instance.PopPage();
        }
    }
}
