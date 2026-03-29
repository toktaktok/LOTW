using Project.Scripts.Content.UI;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.System.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Scripts.Content.Controller
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Character currentCharacter;

        [Tooltip("상호작용 레이어")]
        [SerializeField] private LayerMask interactableLayerMask;
        
        private readonly Collider[] _hitResults = new Collider[5];
        private PlayerControls _controls;
        private WorldObject _player;
        
        private void OnEnable() => _controls.Enable();
        private void OnDisable() => _controls.Disable();

        private void Awake()
        {
            _controls = new PlayerControls();
            _player = GetComponent<WorldObject>();
        }
        private void Update()
        {
            if(_controls.Player.Interact.WasPressedThisFrame())
                TryInteract();
        }

        private void TryInteract()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                currentCharacter.Position, 
                WorldDefines.InteractionDistance, 
                _hitResults, 
                interactableLayerMask
            );
            if(hitCount == 0)
                return;
            
            IInteractable closestInteractable = null;
            float closestSqrDistance = float.MaxValue;
            
            for(int i=0; i<hitCount; i++)
            {
                Collider hit = _hitResults[i];
                if (hit.TryGetComponent(out IInteractable interactable))
                {
                    float sqrDist = (hit.transform.position - currentCharacter.Position).sqrMagnitude;
                    if (sqrDist < closestSqrDistance)
                    {
                        closestSqrDistance = sqrDist;
                        closestInteractable = interactable;
                    }
                }
            }

            if(closestInteractable != null)
                UIManager.Instance.PushPage<DialogueUI>(UILayer.Popup, ui => ui.Setup(closestInteractable, this.gameObject));
        }
    }
}