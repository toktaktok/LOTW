using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Project.Scripts.System.UI;
using Project.Scripts.Data;

namespace Project.Scripts.Core.Managers
{
    public class UIPage
    {
        public UILayer Layer { get; private set; }
        public List<BaseUI> UIComponents { get; private set; } = new List<BaseUI>();

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

        // HudUI.prefab CanvasScaler 설정과 동일
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
            UIPage newPage = new UIPage(layer);
            T ui = GetOrCreateUI(typeof(T), layer) as T;

            if(ui != null)
            {
                setup?.Invoke(ui);

                // 이미 열린 UI는 내용만 갱신하고 페이지를 중복으로 쌓지 않음
                if(ui.IsVisible)
                    return;

                newPage.Add(ui);
                ui.transform.SetAsLastSibling();
                await ui.ShowAsync();

                _pageNavigationStack.Push(newPage);
            }
        }

        private async Awaitable ProcessPushPageGroup(UILayer layer, Type[] uiTypes)
        {
            UIPage newPage = new UIPage(layer);
            List<Awaitable> tasks = new List<Awaitable>();

            foreach(var type in uiTypes)
            {
                BaseUI ui = GetOrCreateUI(type, layer);
                if(ui != null)
                {
                    newPage.Add(ui);
                    ui.transform.SetAsLastSibling();
                    tasks.Add(ui.ShowAsync());
                }
            }

            foreach(var task in tasks)
                await task;
            _pageNavigationStack.Push(newPage);
        }

        private async Awaitable ProcessPopPage()
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
        }

        private async Awaitable ProcessClearAll()
        {
            while(_pageNavigationStack.Count > 0)
            {
                await ProcessPopPage();
            }
        }

        private BaseUI GetOrCreateUI(Type type, UILayer layer)
        {
            if(_uiCache.TryGetValue(type, out BaseUI cached))
                return cached;

            BaseUI prefab = Array.Find(uiPrefabs, p => p != null && p.GetType() == type);
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