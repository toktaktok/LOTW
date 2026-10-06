using Project.Scripts.Content.Story;
using Project.Scripts.Content.UI;
using Project.Scripts.Data;
using Project.Scripts.System.World;
using UnityEngine;
using Project.Scripts.Framework.Managers;

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
            // 막힌 동안은 안내를 숨김 (미니게임 창 아래에 "버튼 누르기" 같은 안내가 남지 않게). 풀리면 다시 찾음
            if(UIManager.Instance.HasBlockingPage || SequencePlayer.IsPlaying)
            {
                SetNearby(null);
                return;
            }

            // 페이드/로딩 중 상호작용하면 사라질 씬의 대상으로 UI가 열림
            if(SceneTransitionManager.HasInstance && SceneTransitionManager.Instance.IsTransitioning)
                return;

            DetectNearbyInteractable();

            if(_controls.Player.Interact.WasPressedThisFrame())
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

            for(int i = 0; i < hitCount; i++)
            {
                if(_hitResults[i].TryGetComponent(out IInteractable interactable) && interactable.CanInteract)
                {
                    float sqrDist = (_hitResults[i].transform.position - currentCharacter.Position).sqrMagnitude;
                    if(sqrDist < closestSqrDist)
                    {
                        closestSqrDist = sqrDist;
                        closest = interactable;
                    }
                }
            }

            SetNearby(closest);
        }

        private void SetNearby(IInteractable nearby)
        {
            if(nearby == _currentNearby)
                return;

            _currentNearby = nearby;
            UpdateHint();
        }

        private void UpdateHint()
        {
            if(_hud == null)
                _hud = UIManager.Instance.Get<HudUI>();

            if(_hud == null)
                return;

            if(_currentNearby != null)
                _hud.ShowInteractionHint(_currentNearby.InteractionPrompt);
            else
                _hud.HideInteractionHint();
        }

        /// <summary>
        /// 대상별 동작은 각 IInteractable이 결정합니다 (RailConnector: 이동, NPC: 대화 UI 등).
        /// </summary>
        private void TryInteract()
        {
            if(_currentNearby == null)
                return;

            _currentNearby.Interact(gameObject);
        }
    }
}
