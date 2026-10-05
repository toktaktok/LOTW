using UnityEngine;

namespace Project.Scripts.System.World
{
    public interface IInteractable
    {
        public void Interact(GameObject interactor);
        
        string InteractionPrompt { get; }

        /// <summary>false면 PlayerInteractor가 대상에서 제외합니다 (힌트도 표시되지 않음).</summary>
        bool CanInteract => true;
    }
}