using System;
using System.IO;
using UnityEngine;
using Project.Scripts.Data;

namespace Project.Scripts.Core.Managers
{
    /// <summary>
    /// 게임 진행 상태를 JSON 파일로 저장/로드합니다.
    /// 슬롯 기반 세이브를 지원합니다.
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        #region Constants

        private const string SaveFolder = "Saves";
        private const string SaveFileFormat = "save_{0}.json";
        public const int MaxSaveSlots = 3;

        #endregion

        #region Events

        public static event Action<int> OnGameSaved;
        public static event Action<int> OnGameLoaded;

        #endregion

        #region Public API

        public void Save(int slot, SaveData data)
        {
            if (slot < 0 || slot >= MaxSaveSlots)
            {
                Debug.LogWarning($"[SaveManager] Invalid save slot: {slot}");
                return;
            }

            data.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string json = JsonUtility.ToJson(data, true);
            string path = GetSavePath(slot);

            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, json);
            Debug.Log($"[SaveManager] Saved to slot {slot}: {path}");

            GameInstance.Instance.SelectSaveSlot(slot);
            OnGameSaved?.Invoke(slot);
        }

        public SaveData? Load(int slot)
        {
            if (slot < 0 || slot >= MaxSaveSlots)
            {
                Debug.LogWarning($"[SaveManager] Invalid save slot: {slot}");
                return null;
            }

            string path = GetSavePath(slot);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveManager] No save file at slot {slot}");
                return null;
            }

            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            GameInstance.Instance.SelectSaveSlot(slot);
            OnGameLoaded?.Invoke(slot);

            return data;
        }

        public bool HasSave(int slot)
        {
            return File.Exists(GetSavePath(slot));
        }

        public void DeleteSave(int slot)
        {
            string path = GetSavePath(slot);
            if (File.Exists(path))
                File.Delete(path);
        }

        public SaveData? PeekSave(int slot)
        {
            string path = GetSavePath(slot);
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<SaveData>(json);
        }

        #endregion

        #region Helpers

        private string GetSavePath(int slot)
        {
            return Path.Combine(Application.persistentDataPath, SaveFolder,
                string.Format(SaveFileFormat, slot));
        }

        #endregion
    }
}
