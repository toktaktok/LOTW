using System;

namespace Project.Scripts.Core.Managers
{
    /// <summary>
    /// 게임 진행 플래그를 보관하는 매니저. 순수 로직은 FlagStore에 위임합니다.
    /// SaveData.flags 필드의 생산자/소비자 역할을 하며,
    /// NPC/트리거 등 콘텐츠가 상호 직접 참조 없이 진행 상태를 공유하는 채널을 제공합니다.
    /// </summary>
    public class FlagManager : Singleton<FlagManager>
    {
        #region Events

        /// <summary>플래그가 변경될 때 발생합니다. 인자는 변경된 키.</summary>
        public static event Action<string> OnFlagChanged;

        #endregion

        #region Fields

        private readonly FlagStore _store = new();

        #endregion

        #region Public API

        public void Set(string key, int value = 1)
        {
            _store.Set(key, value);
            OnFlagChanged?.Invoke(key);
        }

        public bool Has(string key) => _store.Has(key);

        public int Get(string key) => _store.Get(key);

        public int Add(string key, int amount = 1)
        {
            int result = _store.Add(key, amount);
            OnFlagChanged?.Invoke(key);
            return result;
        }

        public void Clear(string key)
        {
            _store.Clear(key);
            OnFlagChanged?.Invoke(key);
        }

        #endregion

        #region Save/Load Integration

        public string[] ToSaveData() => _store.ToSaveData();

        public void LoadFromSaveData(string[] data) => _store.LoadFromSaveData(data);

        #endregion
    }
}
