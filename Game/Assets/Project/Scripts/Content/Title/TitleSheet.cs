using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Project.Scripts.Data;

namespace Project.Scripts.Content.Title
{
    /// <summary>
    /// 서류가방 안의 종이 한 장. 선택되면(키보드 이동 또는 마우스 올림) 살짝 올라옵니다.
    /// 레이아웃 그룹 없이 직접 배치한 RectTransform 에 붙입니다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TitleSheet : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        private RectTransform _rect;
        private Button _button;
        private Vector2 _basePosition;
        private bool _selected;

        // 부모 TitleUI.Awake 가 이 Awake 보다 먼저 불릴 수 있어 처음 접근할 때 가져옴
        public Button Button => _button != null ? _button : _button = GetComponent<Button>();

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _button = GetComponent<Button>();
            _basePosition = _rect.anchoredPosition;
        }

        // 부모가 먼저 선택을 잡은 뒤 이 종이가 켜지면 OnSelect 를 받지 못하므로 현재 선택에서 맞춤
        private void OnEnable()
        {
            _selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        }

        private void OnDisable()
        {
            _selected = false;
            if(_rect != null)
                _rect.anchoredPosition = _basePosition;
        }

        private void Update()
        {
            Vector2 target = _basePosition + (_selected ? Vector2.up * MenuDefines.SheetRaise : Vector2.zero);
            float t = 1f - Mathf.Exp(-MenuDefines.SheetRaiseSpeed * Time.unscaledDeltaTime);
            _rect.anchoredPosition = Vector2.Lerp(_rect.anchoredPosition, target, t);
        }

        public void OnSelect(BaseEventData eventData) => _selected = true;

        public void OnDeselect(BaseEventData eventData) => _selected = false;

        // 마우스와 키보드 선택을 하나로 맞춤
        public void OnPointerEnter(PointerEventData eventData)
        {
            if(_button.interactable && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
