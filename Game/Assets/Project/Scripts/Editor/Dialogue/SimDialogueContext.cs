#if UNITY_EDITOR
using System.Collections.Generic;
using Project.Scripts.System.Dialogue;

namespace Project.Scripts.Editor.Dialogue
{
    /// <summary>
    /// 에디터 시뮬레이션용 게임 상태. 플래그와 인벤토리를 Dictionary 로 보관하고,
    /// 상태를 바꾸거나 사운드를 재생한 내용을 Log 에 순서대로 남깁니다.
    /// </summary>
    public class SimDialogueContext : IDialogueContext
    {
        private readonly Dictionary<string, int> _flags;
        private readonly Dictionary<string, int> _items;
        private readonly List<string> _log = new();

        public SimDialogueContext(IDictionary<string, int> flags, IDictionary<string, int> items)
        {
            _flags = new Dictionary<string, int>(flags);
            _items = new Dictionary<string, int>(items);
        }

        public IReadOnlyDictionary<string, int> Flags => _flags;
        public IReadOnlyDictionary<string, int> Items => _items;
        public IReadOnlyList<string> Log => _log;

        public void AddLog(string message) => _log.Add(message);

        public int GetFlag(string key) => _flags.TryGetValue(key, out int value) ? value : 0;

        public void SetFlag(string key, int value)
        {
            _flags[key] = value;
            _log.Add($"flag {key} = {value}");
        }

        public void AddFlag(string key, int amount)
        {
            _flags[key] = GetFlag(key) + amount;
            _log.Add($"flag {key} += {amount} ({_flags[key]})");
        }

        public int GetItemCount(string itemId) => _items.TryGetValue(itemId, out int count) ? count : 0;

        public bool AddItem(string itemId, int amount)
        {
            _items[itemId] = GetItemCount(itemId) + amount;
            _log.Add($"아이템 지급 {itemId} x{amount} ({_items[itemId]})");
            return true;
        }

        public bool RemoveItem(string itemId, int amount)
        {
            // InventoryManager 와 같이 부족하면 빼지 않고 실패
            if(GetItemCount(itemId) < amount)
            {
                _log.Add($"아이템 회수 실패 {itemId} x{amount} (보유 {GetItemCount(itemId)})");
                return false;
            }

            _items[itemId] = GetItemCount(itemId) - amount;
            _log.Add($"아이템 회수 {itemId} x{amount} ({_items[itemId]})");
            return true;
        }

        public void PlaySfx(string key) => _log.Add($"sfx {key}");
        public void PlayBgm(string key) => _log.Add($"bgm {key}");
    }
}
#endif
