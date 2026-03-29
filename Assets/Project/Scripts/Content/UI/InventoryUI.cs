using UnityEngine;
using UnityEngine.UI;
using Project.Scripts.Core.Managers;
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
        [SerializeField] private Button closeButton;

        protected override void Awake()
        {
            base.Awake();
            closeButton?.onClick.AddListener(OnClose);
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

        public void Refresh()
        {
            // UI 슬롯 갱신은 프리팹 구성에 따라 구현합니다.
            // InventoryManager.Instance.Slots 를 순회하며 각 슬롯 UI를 업데이트하세요.
        }

        private void OnClose()
        {
            UIManager.Instance.PopPage();
        }
    }
}
