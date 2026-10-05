using TMPro;
using UnityEngine;
using Project.Scripts.Core;

namespace Project.Scripts.System.UI
{
    /// <summary>
    /// 고정 라벨(탭 이름, 버튼 글자)에 '@키' 를 붙여 두면 켜질 때와 언어 설정이 바뀔 때 현재 언어로 바꿉니다.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("Text 테이블 키. '@' 포함 (예: @ui.notebook.tab.cover)")]
        [SerializeField] private string key;

        private void OnEnable()
        {
            GameInstance.OnSettingsChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            GameInstance.OnSettingsChanged -= Apply;
        }

        public void SetKey(string newKey)
        {
            key = newKey;
            Apply();
        }

        private void Apply()
        {
            if(!string.IsNullOrEmpty(key))
                GetComponent<TMP_Text>().text = Localization.Resolve(key);
        }
    }
}
