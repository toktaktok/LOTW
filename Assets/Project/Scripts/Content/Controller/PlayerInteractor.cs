using Project.Scripts.Content.UI;
using Project.Scripts.Core.Managers;
using Project.Scripts.Data;
using Project.Scripts.System.World;
using UnityEngine;

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
        private HudUI _hud;

        private IInteractable _currentNearby;

        private void OnEnable() => _controls.Enable();
        private void OnDisable() => _controls.Disable();

        private void Awake()
        {
            _controls = new PlayerControls();
            _player = GetComponent<WorldObject>();
        }

        private void Update()
        {
            DetectNearbyInteractable();

            if (_controls.Player.Interact.WasPressedThisFrame())
                TryInteract();
        }

        private void DetectNearbyInteractable()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                currentCharacter.Position,
                WorldDefines.InteractionDistance,
                _hitResults,
                interactableLayerMask
            );

            IInteractable closest = null;
            float closestSqrDist = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                if (_hitResults[i].TryGetComponent(out IInteractable interactable))
                {
                    float sqrDist = (_hitResults[i].transform.position - currentCharacter.Position).sqrMagnitude;
                    if (sqrDist < closestSqrDist)
                    {
                        closestSqrDist = sqrDist;
                        closest = interactable;
                    }
                }
            }

            if (closest != _currentNearby)
            {
                _currentNearby = closest;
                UpdateHint();
            }
        }

        private void UpdateHint()
        {
            if (_hud == null)
                _hud = FindFirstObjectByType<HudUI>();

            if (_hud == null) return;

            if (_currentNearby != null)
                _hud.ShowInteractionHint(_currentNearby.InteractionPrompt);
            else
                _hud.HideInteractionHint();
        }

        private void TryInteract()
        {
            if (_currentNearby != null)
            {
                UIManager.Instance.PushPage<DialogueUI>(UILayer.Popup,
                    ui => ui.Setup(_currentNearby, gameObject));
            }
        }
    }
}
