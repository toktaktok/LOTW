using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Project.Scripts.Data;

namespace Project.Scripts.Core.Managers
{
    /// <summary>
    /// 임의의 직렬화 가능 데이터를 JSON 파일로 저장/로드하는 범용 유틸리티.
    /// Singleton이 아니므로 여러 데이터 타입에 대해 독립적으로 사용 가능합니다.
    /// </summary>
    public class SaveSystem<T> where T : struct
    {
        private readonly string _saveFolder;
        private readonly string _fileFormat;
        private readonly int _maxSlots;

        public int MaxSlots => _maxSlots;

        public SaveSystem(string saveFolder = "Saves", string fileFormat = "save_{0}.json", int maxSlots = 3)
        {
            _saveFolder = saveFolder;
            _fileFormat = fileFormat;
            _maxSlots = maxSlots;
        }

        public bool Save(int slot, T data)
        {
            if(!ValidateSlot(slot))
                return false;

            string json = JsonUtility.ToJson(data, true);
            string path = GetSavePath(slot);

            string dir = Path.GetDirectoryName(path);
            if(!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, json);
            return true;
        }

        public T? Load(int slot)
        {
            if(!ValidateSlot(slot))
                return null;

            string path = GetSavePath(slot);
            if(!File.Exists(path))
                return null;

            try
            {
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<T>(json);
            }
            catch(Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to load slot {slot}: {ex.Message}");
                return null;
            }
        }

        public bool HasSave(int slot)
        {
            return ValidateSlot(slot) && File.Exists(GetSavePath(slot));
        }

        public void DeleteSave(int slot)
        {
            if(!ValidateSlot(slot))
                return;
            string path = GetSavePath(slot);
            if(File.Exists(path))
                File.Delete(path);
        }

        private bool ValidateSlot(int slot) => slot >= 0 && slot < _maxSlots;

        private string GetSavePath(int slot)
        {
            return Path.Combine(Application.persistentDataPath, _saveFolder,
                string.Format(_fileFormat, slot));
        }
    }

    /// <summary>
    /// 게임 세이브 데이터에 특화된 SaveManager 싱글턴.
    /// SaveSystem&lt;SaveData&gt;를 내부에서 사용하며, GameInstance 슬롯 연동과 이벤트를 추가합니다.
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        #region Constants

        public const int MaxSaveSlots = 3;

        #endregion

        #region Events

        public static event Action<int> OnGameSaved;
        public static event Action<int> OnGameLoaded;

        #endregion

        #region Fields

        private SaveSystem<SaveData> _saveSystem;

        #endregion

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _saveSystem = new SaveSystem<SaveData>(maxSlots: MaxSaveSlots);
        }

        #endregion

        #region Public API

        public void Save(int slot, SaveData data)
        {
            data.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if(_saveSystem.Save(slot, data))
            {
                Debug.Log($"[SaveManager] Saved to slot {slot}");
                GameInstance.Instance.SelectSaveSlot(slot);
                OnGameSaved?.Invoke(slot);
            }
        }

        public SaveData? Load(int slot)
        {
            SaveData? data = _saveSystem.Load(slot);
            if(data.HasValue)
            {
                GameInstance.Instance.SelectSaveSlot(slot);
                OnGameLoaded?.Invoke(slot);
            }
            return data;
        }

        /// <summary>
        /// 현재 게임 상태(씬, 입구, 인벤토리, 플래그)를 모아 슬롯에 저장합니다.
        /// </summary>
        public void SaveCurrent(int slot)
        {
            var data = new SaveData
            {
                currentScene = SceneManager.GetActiveScene().name,
                entranceId = SceneTransitionManager.HasInstance ? SceneTransitionManager.Instance.CurrentEntranceId : "",
                inventory = InventoryManager.Instance.ToSaveData(),
                flags = FlagManager.Instance.ToSaveData()
            };
            Save(slot, data);
        }

        /// <summary>
        /// 슬롯을 로드해 인벤토리와 플래그를 복원하고 저장된 씬/입구로 전환합니다.
        /// 세이브가 없으면 false.
        /// </summary>
        public bool LoadAndApply(int slot)
        {
            SaveData? loaded = Load(slot);
            if(!loaded.HasValue)
                return false;

            SaveData data = loaded.Value;
            InventoryManager.Instance.LoadFromSaveData(data.inventory);
            FlagManager.Instance.LoadFromSaveData(data.flags);

            if(!string.IsNullOrEmpty(data.currentScene))
                SceneTransitionManager.Instance.TransitionTo(data.currentScene, data.entranceId);
            return true;
        }

        public bool HasSave(int slot) => _saveSystem.HasSave(slot);
        public void DeleteSave(int slot) => _saveSystem.DeleteSave(slot);
        public SaveData? PeekSave(int slot) => _saveSystem.Load(slot);

        #endregion
    }
}
