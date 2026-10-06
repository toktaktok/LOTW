using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using Project.Scripts.Framework.UI;

namespace Project.Scripts.Framework.Managers
{
    public class UIPage
    {
        public UILayer Layer { get; private set; }
        public List<BaseUI> UIComponents { get; private set; } = new List<BaseUI>();
        /// <summary>다른 페이지에 덮이는 순간의 선택. 다시 최상단이 되면 복원.</summary>
        public GameObject SelectedObject { get; set; }

        public UIPage(UILayer layer)
        {
            Layer = layer;
        }

        public void Add(BaseUI ui) => UIComponents.Add(ui);
    }

    public class UIManager : Singleton<UIManager>
    {
        #region Settings & Cache

        [Header("Canvas Roots")][Tooltip("Enums.UILayer 순서대로 Transform 할당")]
        [SerializeField]
        private Transform[] layerParents;

        [Header("UI Prefabs")][Tooltip("PushPage로 여는 UI 프리팹 (타입으로 검색)")]
        [SerializeField]
        private BaseUI[] uiPrefabs;

        private Dictionary<Type, BaseUI> _uiCache = new Dictionary<Type, BaseUI>();
        private Stack<UIPage> _pageNavigationStack = new Stack<UIPage>();
        private Queue<Func<Awaitable>> _uiWaitingQueue = new Queue<Func<Awaitable>>();

        private bool _isProcessing = false;

        // PF_HudUI.prefab CanvasScaler 설정과 동일
        private const int LayerSortingOrderStep = 10;
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private const float MatchWidthOrHeight = 0.5f;

        #endregion

        /// <summary>
        /// HUD 레이어를 제외한 페이지가 열려 있거나 UI 작업이 처리 중인지 여부. 플레이어 입력 차단에 사용.
        /// 처리 중도 포함해야 Pop 직후 같은 프레임의 입력이 프롬프트를 다시 열지 않음.
        /// </summary>
        public bool HasBlockingPage
        {
            get
            {
                if(_isProcessing)
                    return true;

                foreach(var page in _pageNavigationStack)
                {
                    if(page.Layer != UILayer.HUD)
                        return true;
                }
                return false;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if(Instance != this)
                return;

            EnsureLayerParents();
        }

        #region Methods

        private void EnsureLayerParents()
        {
            int layerCount = Enum.GetValues(typeof(UILayer)).Length;
            if(layerParents != null && layerParents.Length >= layerCount)
                return;

            Transform[] parents = new Transform[layerCount];
            for(int i = 0; i < layerCount; i++)
            {
                if(layerParents != null && i < layerParents.Length && layerParents[i] != null)
                {
                    parents[i] = layerParents[i];
                    continue;
                }

                UILayer layer = (UILayer)i;
                GameObject root = new GameObject($"[UILayer] {layer}");
                root.transform.SetParent(transform, false);

                Canvas canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = i * LayerSortingOrderStep;

                CanvasScaler scaler = root.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = ReferenceResolution;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = MatchWidthOrHeight;

                root.AddComponent<GraphicRaycaster>();
                parents[i] = root.transform;
            }
            layerParents = parents;
        }

        /// <summary>이미 생성된 UI 인스턴스. 한 번도 열리지 않았으면 null.</summary>
        public T Get<T>() where T : BaseUI
        {
            return _uiCache.TryGetValue(typeof(T), out BaseUI ui) ? ui as T : null;
        }

        /// <summary>UI 열기/닫기 작업이 처리 중인지 여부.</summary>
        public bool IsProcessing => _isProcessing;

        /// <summary>최상단 페이지의 마지막 UI. 페이지가 없거나 최상단이 HUD 면 null.</summary>
        public BaseUI GetTopUI()
        {
            if(_pageNavigationStack.Count == 0)
                return null;

            UIPage top = _pageNavigationStack.Peek();
            if(top.Layer == UILayer.HUD || top.UIComponents.Count == 0)
                return null;
            return top.UIComponents[top.UIComponents.Count - 1];
        }

        public bool IsOpen<T>() where T : BaseUI
        {
            T ui = Get<T>();
            return ui != null && ui.IsVisible;
        }

        /// <summary>T 가 최상단 페이지에 있을 때만 닫는다. 다른 페이지가 위에 있으면 무시.</summary>
        public void Close<T>() where T : BaseUI
        {
            EnqueueOperation(async () =>
            {
                if(_pageNavigationStack.Count == 0)
                    return;

                T ui = Get<T>();
                if(ui == null || !_pageNavigationStack.Peek().UIComponents.Contains(ui))
                    return;

                await ProcessPopPage();
            });
        }

        public void PushPage<T>(UILayer layer = UILayer.Popup) where T : BaseUI
        {
            EnqueueOperation(async () => { await ProcessPushPage<T>(layer, null); });
        }

        public void PushPage<T>(UILayer layer, Action<T> setup) where T : BaseUI
        {
            EnqueueOperation(async () => { await ProcessPushPage<T>(layer, setup); });
        }

        public void PushPageGroup(UILayer layer, params Type[] uiTypes)
        {
            EnqueueOperation(async () => { await ProcessPushPageGroup(layer, uiTypes); });
        }

        public void PopPage()
        {
            EnqueueOperation(async () => { await ProcessPopPage(); });
        }

        public void ClearAllPages()
        {
            EnqueueOperation(async () => { await ProcessClearAll(); });
        }

        private void EnqueueOperation(Func<Awaitable> operation)
        {
            _uiWaitingQueue.Enqueue(operation);

            if(!_isProcessing)
            {
                ProcessQueue().Forget();
            }
        }

        private async Awaitable ProcessQueue()
        {
            if(_isProcessing)
                return;
            _isProcessing = true;

            try
            {
                while(_uiWaitingQueue.Count > 0)
                {
                    Func<Awaitable> operation = _uiWaitingQueue.Dequeue();

                    try
                    {
                        await operation.Invoke();
                    }
                    catch(Exception ex)
                    {
                        Debug.LogError($"[UIManager] Error processing UI operation: {ex}");
                    }
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private async Awaitable ProcessPushPage<T>(UILayer layer, Action<T> setup) where T : BaseUI
        {
            // 생성(OnEnable)이나 setup 이 선택을 바꾸기 전에 아래 페이지의 선택을 잡아 둠
            GameObject belowSelection = CurrentSelection();
            UIPage newPage = new UIPage(layer);
            T ui = GetOrCreateUI(typeof(T), layer) as T;

            if(ui != null)
            {
                setup?.Invoke(ui);

                // 이미 열린 UI는 내용만 갱신하고 페이지를 중복으로 쌓지 않음
                if(ui.IsVisible)
                    return;

                newPage.Add(ui);
                CoverTopPage(newPage, belowSelection);
                ui.transform.SetAsLastSibling();
                await ui.ShowAsync();

                _pageNavigationStack.Push(newPage);
            }
        }

        private async Awaitable ProcessPushPageGroup(UILayer layer, Type[] uiTypes)
        {
            GameObject belowSelection = CurrentSelection();
            UIPage newPage = new UIPage(layer);
            List<Awaitable> tasks = new List<Awaitable>();

            foreach(var type in uiTypes)
            {
                BaseUI ui = GetOrCreateUI(type, layer);
                if(ui != null)
                    newPage.Add(ui);
            }

            CoverTopPage(newPage, belowSelection);
            foreach(var ui in newPage.UIComponents)
            {
                ui.transform.SetAsLastSibling();
                tasks.Add(ui.ShowAsync());
            }

            foreach(var task in tasks)
                await task;
            _pageNavigationStack.Push(newPage);
        }

        private async Awaitable ProcessPopPage(bool restoreBelow = true)
        {
            if(_pageNavigationStack.Count == 0)
                return;

            UIPage currentPage = _pageNavigationStack.Pop();
            List<Awaitable> tasks = new List<Awaitable>();

            foreach(var ui in currentPage.UIComponents)
            {
                tasks.Add(ui.HideAsync());
            }

            foreach(var task in tasks)
                await task;

            if(restoreBelow && _pageNavigationStack.Count > 0)
                UncoverTopPage();
        }

        private async Awaitable ProcessClearAll()
        {
            while(_pageNavigationStack.Count > 0)
            {
                await ProcessPopPage(false);
            }
        }

        /// <summary>새 페이지가 열리기 직전, 현재 최상단 페이지의 선택을 기억하고 상호작용을 끈다(키보드 이동이 아래 페이지로 새는 것 방지).</summary>
        private static GameObject CurrentSelection()
        {
            return EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        }

        private void CoverTopPage(UIPage newPage, GameObject selected)
        {
            if(_pageNavigationStack.Count == 0)
                return;

            UIPage top = _pageNavigationStack.Peek();
            top.SelectedObject = selected;
            foreach(var ui in top.UIComponents)
            {
                if(!newPage.UIComponents.Contains(ui))
                    ui.SetInteractable(false);
            }
        }

        /// <summary>위 페이지가 닫힌 뒤 다시 최상단이 된 페이지의 상호작용과 선택을 되돌림.</summary>
        private void UncoverTopPage()
        {
            UIPage top = _pageNavigationStack.Peek();
            foreach(var ui in top.UIComponents)
            {
                if(ui.IsVisible)
                    ui.SetInteractable(true);
            }

            GameObject selected = top.SelectedObject;
            if(selected != null && selected.activeInHierarchy && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(selected);
        }

        private BaseUI GetOrCreateUI(Type type, UILayer layer)
        {
            if(_uiCache.TryGetValue(type, out BaseUI cached))
                return cached;

            BaseUI prefab = uiPrefabs == null ? null : Array.Find(uiPrefabs, p => p != null && p.GetType() == type);
            if(prefab == null)
            {
                Debug.LogError($"[UIManager] UI prefab not registered: {type.Name}");
                return null;
            }

            BaseUI ui = Instantiate(prefab, layerParents[(int)layer]);
            _uiCache.Add(type, ui);
            return ui;
        }

        #endregion
    }
}