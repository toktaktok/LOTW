using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Project.Scripts.Core;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.System.UI;

namespace Project.Scripts.Content.UI
{
    /// <summary>
    /// 설정 화면: 마스터/BGM/SFX 볼륨, 언어. 값은 GameInstance 가 PlayerPrefs 에 바로 저장합니다.
    /// 타이틀과 일시정지 메뉴 양쪽에서 엽니다.
    /// </summary>
    public class SettingsUI : BaseUI
    {
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Button languageButton;
        [SerializeField] private Button closeButton;

        protected override void Awake()
        {
            base.Awake();
            if(masterSlider != null)
                masterSlider.onValueChanged.AddListener(v => GameInstance.Instance.SetMasterVolume(v));
            if(bgmSlider != null)
                bgmSlider.onValueChanged.AddListener(v => GameInstance.Instance.SetBgmVolume(v));
            if(sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(v => GameInstance.Instance.SetSfxVolume(v));
            if(languageButton != null)
                languageButton.onClick.AddListener(OnLanguage);
            if(closeButton != null)
                closeButton.onClick.AddListener(() => UIManager.Instance.Close<SettingsUI>());
        }

        private void OnEnable()
        {
            GameInstance settings = GameInstance.Instance;
            if(masterSlider != null)
                masterSlider.SetValueWithoutNotify(settings.MasterVolume);
            if(bgmSlider != null)
                bgmSlider.SetValueWithoutNotify(settings.BgmVolume);
            if(sfxSlider != null)
                sfxSlider.SetValueWithoutNotify(settings.SfxVolume);
            if(masterSlider != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(masterSlider.gameObject);
        }

        // 언어 이름 라벨은 LocalizedText(@ui.language.name)라 바꾸는 즉시 새 언어 이름으로 보임
        public override bool OnCancel()
        {
            UIManager.Instance.Close<SettingsUI>();
            return true;
        }

        private void OnLanguage()
        {
            string[] languages = MenuDefines.Languages;
            int index = Array.IndexOf(languages, GameInstance.Instance.Language);
            GameInstance.Instance.SetLanguage(languages[(index + 1) % languages.Length]);
        }
    }
}
