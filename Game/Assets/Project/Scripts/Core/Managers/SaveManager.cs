using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Project.Scripts.Data;

namespace Project.Scripts.Core.Managers
{
    /// <summary>
    /// 게임 세이브 데이터에 특화된 SaveManager 싱글턴.
    /// SaveSystem&lt;SaveData&gt;를 내부에서 사용하며, GameInstance 슬롯 연동, 플레이 시간, 새 게임 초기화를 담당합니다.
    /// 스토리 진행(의뢰/수첩/만남)은 플래그에 있으므로 flags 만으로 함께 저장됩니다.
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        #region Events

        public static event Action<int> OnGameSaved;
        public static event Action<int> OnGameLoaded;

        #endregion

        #region Fields

        private SaveSystem<SaveData> _saveSystem;
        private float _playTime;

        #endregion

        #region Properties

        /// <summary>현재 세션 누적 플레이 시간(초). 일시정지(timeScale 0) 중에는 늘지 않습니다.</summary>
        public float PlayTime => _playTime;

        #endregion

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _saveSystem = new SaveSystem<SaveData>(maxSlots: SaveDefines.MaxSlots);
        }

        private void Update()
        {
            _playTime += Time.deltaTime;
        }

        #endregion

        #region Public API

        public bool Save(int slot, SaveData data)
        {
            data.version = SaveDefines.Version;
            data.playTime = _playTime;
            data.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if(!_saveSystem.Save(slot, data))
                return false;

            GameInstance.Instance.SelectSaveSlot(slot);
            OnGameSaved?.Invoke(slot);
            return true;
        }

        public SaveData? Load(int slot)
        {
            SaveData? data = _saveSystem.Load(slot);
            if(!data.HasValue)
                return null;

            GameInstance.Instance.SelectSaveSlot(slot);
            OnGameLoaded?.Invoke(slot);
            return Upgrade(data.Value);
        }

        /// <summary>
        /// 현재 게임 상태(씬, 입구, 인벤토리, 플래그)를 모아 슬롯에 저장합니다.
        /// </summary>
        public bool SaveCurrent(int slot)
        {
            var data = new SaveData
            {
                currentScene = SceneManager.GetActiveScene().name,
                entranceId = SceneTransitionManager.HasInstance ? SceneTransitionManager.Instance.CurrentEntranceId : "",
                inventory = InventoryManager.Instance.ToSaveData(),
                flags = FlagManager.Instance.ToSaveData()
            };
            return Save(slot, data);
        }

        /// <summary>
        /// 슬롯을 로드해 인벤토리, 플래그, 플레이 시간을 복원하고 저장된 씬/입구로 전환합니다.
        /// 불러온 횟수(StoryKeys.LoadCount)를 1 올립니다. 세이브가 없거나 읽지 못하면 false.
        /// </summary>
        public bool LoadAndApply(int slot)
        {
            SaveData? loaded = Load(slot);
            if(!loaded.HasValue)
                return false;

            SaveData data = loaded.Value;
            _playTime = data.playTime;
            InventoryManager.Instance.LoadFromSaveData(data.inventory);
            FlagManager.Instance.LoadFromSaveData(data.flags);
            FlagManager.Instance.Add(StoryKeys.LoadCount);

            if(!string.IsNullOrEmpty(data.currentScene))
                SceneTransitionManager.Instance.TransitionTo(data.currentScene, data.entranceId);
            return true;
        }

        /// <summary>
        /// 새 게임: 플래그(의뢰/수첩 포함), 인벤토리, 플레이 시간을 비웁니다. 첫 씬 전환은 호출한 쪽(타이틀)이 합니다.
        /// 슬롯 파일은 지우지 않습니다. 처음 저장할 때 덮어씁니다.
        /// </summary>
        public void NewGame(int slot)
        {
            _playTime = 0f;
            FlagManager.Instance.ResetAll();
            InventoryManager.Instance.LoadFromSaveData(null);
            GameInstance.Instance.SelectSaveSlot(slot);
        }

        public bool HasSave(int slot) => _saveSystem.HasSave(slot);
        public void DeleteSave(int slot) => _saveSystem.DeleteSave(slot);

        /// <summary>슬롯 UI 미리보기용. 슬롯 선택/이벤트 없이 읽기만 합니다.</summary>
        public SaveData? PeekSave(int slot)
        {
            SaveData? data = _saveSystem.Load(slot);
            return data.HasValue ? Upgrade(data.Value) : (SaveData?)null;
        }

        /// <summary>
        /// 옛 버전 세이브를 현재 형식으로 맞춥니다. 버전 0 -> 1: 새 필드(version, playTime)는 기본값 그대로.
        /// 더 새 버전(다운그레이드)은 경고만 하고 그대로 씁니다.
        /// </summary>
        public static SaveData Upgrade(SaveData data)
        {
            if(data.version > SaveDefines.Version)
            {
                Debug.LogWarning($"[SaveManager] Save version {data.version} is newer than {SaveDefines.Version}");
                return data;
            }

            data.version = SaveDefines.Version;
            return data;
        }

        #endregion
    }
}
