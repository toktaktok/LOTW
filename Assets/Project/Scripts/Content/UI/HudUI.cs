using TMPro;
using UnityEngine;
using Project.Scripts.Core;
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

        // 씬 전환으로 HUD가 닫혔다 다시 열릴 때 이전 씬의 힌트가 남지 않도록 함
        private void OnDisable() => HideInteractionHint();

        public void ShowInteractionHint(string prompt)
        {
            if(interactionHintRoot == null)
                return;

            interactionHintText.text = Localization.Resolve(prompt);

            if(!_hintVisible)
            {
                interactionHintRoot.SetActive(true);
                _hintVisible = true;
            }
        }

        public void HideInteractionHint()
        {
            if(!_hintVisible || interactionHintRoot == null)
                return;

            interactionHintRoot.SetActive(false);
            _hintVisible = false;
        }
    }
}
