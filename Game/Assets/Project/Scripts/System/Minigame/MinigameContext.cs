using Project.Scripts.System.Dialogue;

namespace Project.Scripts.System.Minigame
{
    /// <summary>
    /// 게임 상태 창구(플래그/아이템/소리)에 미니게임 변수를 얹은 컨텍스트.
    /// 미니게임 조건/액션은 이 컨텍스트로 DialogueCommands를 실행합니다.
    /// </summary>
    public class MinigameContext : IDialogueContext, IVariableContext
    {
        private readonly IDialogueContext _inner;
        private readonly MinigameVars _vars;

        public MinigameContext(IDialogueContext inner, MinigameVars vars)
        {
            _inner = inner;
            _vars = vars;
        }

        public int GetVar(string key) => _vars.Get(key);

        public int GetFlag(string key) => _inner.GetFlag(key);
        public void SetFlag(string key, int value) => _inner.SetFlag(key, value);
        public void AddFlag(string key, int amount) => _inner.AddFlag(key, amount);

        public int GetItemCount(string itemId) => _inner.GetItemCount(itemId);
        public bool AddItem(string itemId, int amount) => _inner.AddItem(itemId, amount);
        public bool RemoveItem(string itemId, int amount) => _inner.RemoveItem(itemId, amount);

        public void PlaySfx(string key) => _inner.PlaySfx(key);
        public void PlayBgm(string key) => _inner.PlayBgm(key);
    }
}
