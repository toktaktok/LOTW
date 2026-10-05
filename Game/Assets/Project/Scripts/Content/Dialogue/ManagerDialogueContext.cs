using Project.Scripts.Content.Story;
using Project.Scripts.Core.Managers;
using Project.Scripts.System.Dialogue;

namespace Project.Scripts.Content.Dialogue
{
    /// <summary>
    /// 대화 조건/액션을 실제 매니저(FlagManager, InventoryManager, AudioManager)로 연결합니다.
    /// </summary>
    public class ManagerDialogueContext : IDialogueContext
    {
        public int GetFlag(string key) => FlagManager.Instance.Get(key);
        public void SetFlag(string key, int value) => FlagManager.Instance.Set(key, value);
        public void AddFlag(string key, int amount) => FlagManager.Instance.Add(key, amount);

        public int GetItemCount(string itemId) => InventoryManager.Instance.GetItemCount(itemId);
        public bool AddItem(string itemId, int amount) => InventoryManager.Instance.AddItem(itemId, amount);
        public bool RemoveItem(string itemId, int amount) => InventoryManager.Instance.RemoveItem(itemId, amount);

        public void PlaySfx(string key) => AudioManager.Instance.PlaySFX(key);
        public void PlayBgm(string key) => AudioManager.Instance.PlayBGM(key);

        public void PlaySequence(int sequenceId) => SequencePlayer.Instance.Play(sequenceId);
    }
}
