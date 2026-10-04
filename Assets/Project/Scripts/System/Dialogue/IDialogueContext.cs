namespace Project.Scripts.System.Dialogue
{
    /// <summary>
    /// DialogueCommands 가 조건을 읽고 액션을 실행하는 게임 상태 창구.
    /// 실제 구현은 매니저(Flag/Inventory/Audio)에 위임하고, 테스트는 가짜 구현을 씁니다.
    /// </summary>
    public interface IDialogueContext
    {
        int GetFlag(string key);
        void SetFlag(string key, int value);
        void AddFlag(string key, int amount);

        int GetItemCount(string itemId);
        bool AddItem(string itemId, int amount);
        bool RemoveItem(string itemId, int amount);

        void PlaySfx(string key);
        void PlayBgm(string key);
    }
}
