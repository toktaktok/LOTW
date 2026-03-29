using System;
using UnityEngine;
using System.Collections.Generic;
using Project.Scripts.System.UI;
using Project.Scripts.Data;

namespace Project.Scripts.Core.Managers
{
    public class UIPage
    {
        public List<BaseUI> UIComponents { get; private set; } = new List<BaseUI>();

        public void Add(BaseUI ui) => UIComponents.Add(ui);
    }

    public class UIManager : Singleton<UIManager>
    {
        #region Settings & Cache

        [Header("Canvas Roots")][Tooltip("Enums.UILayer 순서대로 Transform 할당")]
        [SerializeField]
        private Transform[] layerParents;

        private Dictionary<Type, BaseUI> _uiCache = new Dictionary<Type, BaseUI>();
        private Stack<UIPage> _pageNavigationStack = new Stack<UIPage>();
        private Queue<Func<Awaitable>> _uiWaitingQueue = new Queue<Func<Awaitable>>();

        private bool _isProcessing = false;

        #endregion

        #region Methods

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
                ProcessQueue().Cancel();
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
            UIPage newPage = new UIPage();
            T ui = await GetOrCreateUI<T>(layer);

            if(ui != null)
            {
                setup?.Invoke(ui);
                newPage.Add(ui);
                ui.transform.SetAsLastSibling();
                await ui.ShowAsync();

                _pageNavigationStack.Push(newPage);
            }
        }

        private async Awaitable ProcessPushPageGroup(UILayer layer, Type[] uiTypes)
        {
            UIPage newPage = new UIPage();
            List<Awaitable> tasks = new List<Awaitable>();

            foreach(var type in uiTypes)
            {
                BaseUI ui = await GetOrCreateUI(type, layer);
                if(ui != null)
                {
                    newPage.Add(ui);
                    ui.transform.SetAsLastSibling();
                    tasks.Add(ui.ShowAsync());
                }
            }

            foreach(var task in tasks) await task;
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

            foreach(var task in tasks) await task;
        }

        private async Awaitable ProcessClearAll()
        {
            while(_pageNavigationStack.Count > 0)
            {
                await ProcessPopPage();
            }
        }

        // ... GetOrCreateUI는 이전과 동일 ...
        private async Awaitable<T> GetOrCreateUI<T>(UILayer layer) where T : BaseUI
            => await GetOrCreateUI(typeof(T), layer) as T;

        private async Awaitable<BaseUI> GetOrCreateUI(Type type, UILayer layer)
        {
            if(_uiCache.TryGetValue(type, out BaseUI cached)) return cached;

            var req = Resources.LoadAsync<GameObject>($"UI/{type.Name}");
            while(!req.isDone) await Awaitable.NextFrameAsync();

            if(req.asset == null) return null;

            var go = Instantiate(req.asset as GameObject, layerParents[(int)layer]);
            var ui = go.GetComponent<BaseUI>();
            _uiCache.Add(type, ui);
            return ui;
        }

        #endregion
    }
}