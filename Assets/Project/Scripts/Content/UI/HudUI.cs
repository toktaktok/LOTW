using TMPro;
using UnityEngine;
using Project.Scripts.System.UI;

namespace Project.Scripts.Content.UI
{
    /// <summary>
    /// 메인 게임플레이 HUD.
    /// 상호작용 가능한 오브젝트가 근처에 있을 때 힌트를 표시합니다.
    /// </summary>
    public class HudUI : BaseUI
    {
        [Header("Interaction Hint")]
        [SerializeField] private GameObject interactionHintRoot;
        [SerializeField] private TMP_Text interactionHintText;

        private bool _hintVisible;

        public void ShowInteractionHint(string prompt)
        {
            if (interactionHintRoot == null) return;

            interactionHintText.text = prompt;

            if (!_hintVisible)
            {
                interactionHintRoot.SetActive(true);
                _hintVisible = true;
            }
        }

        public void HideInteractionHint()
        {
            if (!_hintVisible || interactionHintRoot == null) return;

            interactionHintRoot.SetActive(false);
            _hintVisible = false;
        }
    }
}
