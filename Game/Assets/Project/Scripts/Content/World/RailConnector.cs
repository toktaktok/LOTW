using UnityEngine;
using System.Collections.Generic;
using Project.Scripts.Content.Controller;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.System.World;
using Unity.Cinemachine;

namespace Project.Scripts.Content.World
{
    public class RailConnector : MonoBehaviour, IInteractable
    {
        public float radius = 0.5f;
        
        [Header("Connection")]
        [SerializeField] private bool isWarp = true;
        [SerializeField] private RailNode destinationNode;
        [SerializeField] private CinemachineCamera targetCamera;
        [SerializeField] private float blendDuration = 0f;

        [Header("Interaction")]
        [SerializeField] private string promptText = "이동하기";
        [SerializeField] private float interactionDistance = WorldDefines.InteractionDistance;

        public float InteractionDistance => interactionDistance;
        public Vector3 InteractionPosition => transform.position;
        public string InteractionPrompt => promptText;

        /// <summary>맵 빌더가 모든 노드 생성 후 linkedNodeId를 해소해 주입합니다.</summary>
        public void SetDestinationNode(RailNode node) => destinationNode = node;

        public void Interact(GameObject interactor)
        {
            PlayerController pc = interactor.GetComponent<PlayerController>();
            if(pc == null || destinationNode == null)
                return;
            if(TryGetComponent(out IInteractionGate gate) && !gate.TryPass(interactor))
                return;

            if(isWarp)
            {
                pc.SwitchPath(destinationNode);
                if(targetCamera != null)
                    CameraManager.Instance.SwitchCamera(targetCamera, blendDuration);
            }
            else
            {
                pc.MoveToAndSwitchPath(destinationNode, targetCamera, blendDuration);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.softRed;
            Gizmos.DrawSphere(transform.position, radius);

            Gizmos.color = Color.greenYellow;
            Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }
    }
}