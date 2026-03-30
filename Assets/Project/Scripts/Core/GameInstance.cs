using System;
using UnityEngine;
using Project.Scripts.Core.Managers;

namespace Project.Scripts.Core
{
    /// <summary>
    /// 게임 설정에 대한 읽기 전용 인터페이스.
    /// 다른 시스템이 GameInstance에 직접 의존하지 않도록 합니다.
    /// </summary>
    public interface ISettingsProvider : IVolumeProvider
    {
        string Language { get; }
        int CurrentSaveSlot { get; }
    }

    /// <summary>
    /// 게임 전역 상태를 관리합니다.
    /// 게임 설정(볼륨, 언어 등)과 현재 세이브 슬롯 정보를 보관합니다.
    /// ISettingsProvider를 구현하여 읽기 전용 접근을 제공합니다.
    /// </summary>
    public class GameInstance : Singleton<GameInstance>, ISettingsProvider
    {
        #region Events

        public static event Action OnSettingsChanged;

        #endregion

        #region Settings

        private const string PrefKey_MasterVolume = "Settings_MasterVolume";
        private const string PrefKey_BgmVolume = "Settings_BgmVolume";
        private const string PrefKey_SfxVolume = "Settings_SfxVolume";
        private const string PrefKey_Language = "Settings_Language";
        private const string PrefKey_LastSaveSlot = "Settings_LastSaveSlot";

        public float MasterVolume { get; private set; } = 1f;
        public float BgmVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;
        public string Language { get; private set; } = "ko";
        public int CurrentSaveSlot { get; private set; } = -1;

        #endregion

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();
            LoadSettings();
        }

        #endregion

        #region Settings API

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        public void SetBgmVolume(float volume)
        {
            BgmVolume = Mathf.Clamp01(volume);
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        public void SetSfxVolume(float volume)
        {
            SfxVolume = Mathf.Clamp01(volume);
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        public void SetLanguage(string lang)
        {
            Language = lang;
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        public void SelectSaveSlot(int slot)
        {
            CurrentSaveSlot = slot;
            PlayerPrefs.SetInt(PrefKey_LastSaveSlot, slot);
            PlayerPrefs.Save();
        }

        #endregion

        #region Persistence

        private void LoadSettings()
        {
            MasterVolume = PlayerPrefs.GetFloat(PrefKey_MasterVolume, 1f);
            BgmVolume = PlayerPrefs.GetFloat(PrefKey_BgmVolume, 1f);
            SfxVolume = PlayerPrefs.GetFloat(PrefKey_SfxVolume, 1f);
            Language = PlayerPrefs.GetString(PrefKey_Language, "ko");
            CurrentSaveSlot = PlayerPrefs.GetInt(PrefKey_LastSaveSlot, -1);
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(PrefKey_MasterVolume, MasterVolume);
            PlayerPrefs.SetFloat(PrefKey_BgmVolume, BgmVolume);
            PlayerPrefs.SetFloat(PrefKey_SfxVolume, SfxVolume);
            PlayerPrefs.SetString(PrefKey_Language, Language);
            PlayerPrefs.Save();
        }

        #endregion
    }
}
