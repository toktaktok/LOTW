using UnityEngine;
using System.Collections.Generic;
using Project.Scripts.System.World;
using Unity.Cinemachine;

namespace Project.Scripts.Content.World
{
    public class RailConnector : MonoBehaviour, IInteractable
    {
        [Header("Connection Settings")] [SerializeField]
        private bool isWarp = true;

        [SerializeField] private RailNode destinationNode;
        [SerializeField] private CinemachineCamera targetCamera;

        public void Interact(WorldObject interactor)
        {
            
        }
    }
}