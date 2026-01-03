using UnityEngine;

namespace Project.Scripts.System.World
{
    public interface IInteractable
    {
        public void Interact(GameObject interactor);
        
        string InteractionPrompt { get; }
    }
}