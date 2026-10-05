using System;
using System.IO;
using UnityEngine;

namespace Project.Scripts.Core.Managers
{
    /// <summary>
    /// 임의의 직렬화 가능 데이터를 JSON 파일로 저장/로드하는 범용 유틸리티.
    /// Singleton이 아니므로 여러 데이터 타입에 대해 독립적으로 사용 가능합니다.
    /// 임시 파일에 먼저 쓰고 교체하므로 쓰는 도중 종료돼도 기존 세이브가 깨지지 않습니다.
    /// </summary>
    public class SaveSystem<T> where T : struct
    {
        private const string TempSuffix = ".tmp";

        private readonly string _rootPath;
        private readonly string _saveFolder;
        private readonly string _fileFormat;
        private readonly int _maxSlots;

        public int MaxSlots => _maxSlots;

        /// <param name="rootPath">null 이면 Application.persistentDataPath. 테스트는 임시 폴더를 넘깁니다.</param>
        public SaveSystem(string saveFolder = "Saves", string fileFormat = "save_{0}.json", int maxSlots = 3, string rootPath = null)
        {
            _rootPath = rootPath;
            _saveFolder = saveFolder;
            _fileFormat = fileFormat;
            _maxSlots = maxSlots;
        }

        public bool Save(int slot, T data)
        {
            if(!ValidateSlot(slot))
                return false;

            string path = GetSavePath(slot);
            string temp = path + TempSuffix;
            try
            {
                string dir = Path.GetDirectoryName(path);
                if(!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                if(File.Exists(path))
                    File.Replace(temp, path, null);
                else
                    File.Move(temp, path);
                return true;
            }
            catch(Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to save slot {slot}: {ex.Message}");
                TryDelete(temp);
                return false;
            }
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
                if(string.IsNullOrWhiteSpace(json))
                    throw new InvalidDataException("empty file");
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
            if(ValidateSlot(slot))
                TryDelete(GetSavePath(slot));
        }

        private bool ValidateSlot(int slot) => slot >= 0 && slot < _maxSlots;

        private string GetSavePath(int slot)
        {
            return Path.Combine(_rootPath ?? Application.persistentDataPath, _saveFolder, string.Format(_fileFormat, slot));
        }

        private static void TryDelete(string path)
        {
            try
            {
                if(File.Exists(path))
                    File.Delete(path);
            }
            catch(Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Failed to delete {path}: {ex.Message}");
            }
        }
    }
}
