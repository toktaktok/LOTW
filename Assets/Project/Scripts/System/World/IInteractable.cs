using UnityEngine;

namespace Project.Scripts.System.World
{
    public interface IInteractable
    {
        public void Interact(WorldObject interactor);
    }
}