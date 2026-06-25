// UIManager_Addressables.cs
// Full, clean, production-ready UI manager with:
// - Addressables key = typeof(T).Name
// - Auto-create layer roots from UILayer enum
// - Main layer supports:
//    + PageExclusive (Equip/Dungeon/Gacha...): open new page closes all stackables + closes previous page (Rule B)
//    + Stackable (StormView...): opens on top of current page (no page close)
// - Shared backdrop for Main layer
// - Per-entry backdrop for other layers (optional)
// - CacheOnClose: close = PlayHideAsync then SetActive(false), keep instance+handle for fast reopen
// - OpenPopupAsync dedupes concurrent opens per type (anti-spam)
// - CloseTopMain(): closes top stackable first, then page; hides main backdrop if nothing visible
// - CloseTopPopup(): closes most recent popup globally (non-main)
// - InitMainScene(): WhenAll open persistent bottom/top main views

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    public enum UILayer
    {
        Background = 0,
        SubMain = 1,
        Main = 2,
        OverMain = 3,
        Popup = 4,
        Overlay = 5,
        Toast = 6,
        System = 7
    }

    public enum MainLayerMode
    {
        PageExclusive, // Equip/Dungeon/Gacha...
        Stackable // StormView...
    }

    public abstract class UIView : MonoBehaviour
    {
        [Header("UI View")] public UILayer Layer = UILayer.Main;

        [Header("Main Layer Mode (only used if Layer==Main)")]
        public MainLayerMode MainMode = MainLayerMode.PageExclusive;

        [Header("Popup Settings")] public bool CloseOnBackdrop = false;

        [Header("Lifetime")]
        [Tooltip("If true: Close will hide (SetActive(false)) and keep instance+handle for fast reopen.")]
        public bool CacheOnClose = false;

        public virtual void OnShow(object args)
        {
        }

        public virtual void OnHide()
        {
        }

        // Animation hooks (override if needed)
        public virtual UniTask PlayShowAsync(object args)
        {
            OnShow(args);
            return UniTask.CompletedTask;
        }

        public virtual UniTask PlayHideAsync()
        {
            OnHide();
            return UniTask.CompletedTask;
        }
    }

    public sealed class UIManager : Singleton<UIManager>
    {
        [Header("Backdrop Prefab (normal prefab, not Addressables)")] [SerializeField]
        private GameObject backdropPrefab;
        
        [SerializeField]
        private CanvasGroup loadingSceneCanvasGroup;
        
        [SerializeField]
        private GameObject tapeAnimator;

        [SerializeField] 
        private CanvasScaler canvasScaler;

        // ===== Layer roots =====
        private readonly Dictionary<UILayer, RectTransform> _layerRoots = new();

        // ===== Open dedupe =====
        private readonly Dictionary<string, UniTaskCompletionSource<UIView>> _openingTasks = new();

        // ===== Cache (single instance per type) =====
        private sealed class Entry
        {
            public string key;
            public UIView view;
            public UILayer layer;
            public AsyncOperationHandle<GameObject> handle;

            public GameObject backdropInstance; // per-entry backdrop (non-main)
            public bool closed;
            public bool cacheOnClose;
        }

        private readonly Dictionary<string, Entry> _cachedEntries = new();

        // ===== Non-main popup stack =====
        private readonly Dictionary<UILayer, List<Entry>> _entriesByLayer = new();
        private readonly Stack<Entry> _globalPopupStack = new();

        // ===== Main layer state =====
        private Entry _activeMainPage; // current exclusive page
        private readonly Stack<Entry> _mainStack = new(); // stackable views on main
        private GameObject _mainSharedBackdrop; // shared backdrop for main
        private bool _suppressMainBackdropRefresh;

        private void Start()
        {
            GameEventManager.Subscribe(GameEvents.OnInitSceneDataComplete, OnInitSceneDataComplete);

            canvasScaler.matchWidthOrHeight = ScreenOrientationTracker.Instance.CurrentMode ==
                                              ScreenOrientationTracker.ScreenViewMode.Landscape
                ? 0.5f
                : 0f;
            
            ScreenOrientationTracker.Instance.OnOrientationChanged += ScreenViewMode =>
            {
                switch (ScreenViewMode)
                {
                    case ScreenOrientationTracker.ScreenViewMode.Landscape:
                        canvasScaler.matchWidthOrHeight = 0.5f;
                        break;
                    case ScreenOrientationTracker.ScreenViewMode.Portrait:
                        canvasScaler.matchWidthOrHeight = 0f;
                        break;
                }
            };
        }

        public override async UniTask InitializeAsync()
        {
            CreateLayerRootsFromEnum();
            InitLayerLists();
            await InitMainScene();
        }

        #region Layer Roots

        private void CreateLayerRootsFromEnum()
        {
            _layerRoots.Clear();

            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                var go = new GameObject(layer.ToString());
                go.transform.SetParent(transform, false);

                var rt = go.AddComponent<RectTransform>();
                StretchFull(rt);

                rt.SetSiblingIndex((int)layer);
                _layerRoots[layer] = rt;
            }
        }

        private void InitLayerLists()
        {
            _entriesByLayer.Clear();
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
                _entriesByLayer[layer] = new List<Entry>();
        }

        private RectTransform GetLayerRoot(UILayer layer)
        {
            if (_layerRoots.TryGetValue(layer, out var rt) && rt != null)
                return rt;

            Debug.LogError($"[UI] Missing layer root for {layer}");
            return null;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        #endregion

        #region Public Queries

        public bool IsAnyMainVisible()
        {
            if (_activeMainPage != null && !_activeMainPage.closed && _activeMainPage.view != null &&
                _activeMainPage.view.gameObject.activeSelf)
                return true;

            foreach (var e in _mainStack)
            {
                if (e != null && !e.closed && e.view != null && e.view.gameObject.activeSelf)
                    return true;
            }

            return false;
        }

        public bool IsOpen<T>() where T : UIView
        {
            // main page
            if (_activeMainPage != null && !_activeMainPage.closed && _activeMainPage.view is T &&
                _activeMainPage.view.gameObject.activeSelf)
                return true;

            // main stack
            foreach (var e in _mainStack)
            {
                if (e != null && !e.closed && e.view is T && e.view.gameObject.activeSelf)
                    return true;
            }

            // other layers
            foreach (var kv in _entriesByLayer)
            {
                var list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    if (e == null || e.closed || e.view == null) continue;
                    if (e.view is T && e.view.gameObject.activeSelf) return true;
                }
            }

            return false;
        }

        #endregion

        #region Public API

        public async UniTask<bool> TogglePopupAsync<T>(object args = null, bool withBackdrop = true) where T : UIView
        {
            if (IsOpen<T>())
            {
                Close<T>();
                GameEventManager.Trigger(GameEvents.OnToggleMainView);
                return false;
            }

            var view = await OpenPopupAsync<T>(args, withBackdrop);
            GameEventManager.Trigger(GameEvents.OnToggleMainView);
            return view != null;
        }

        /// <summary>
        /// Close a view by type:
        /// - If it's a Main stackable => closes top-most stackable if matches
        /// - Else if it's the active Main page => closes that page
        /// - Else closes most recent popup of that type in global popup stack
        /// </summary>
        public void Close<T>() where T : UIView
        {
            {
                var buffer = new List<Entry>();
                while (_mainStack.Count > 0)
                {
                    var top = _mainStack.Pop();
                    if (top == null || top.closed) continue;

                    if (top.view is T)
                    {
                        CloseEntryAsync(top).Forget();
                        // restore skipped
                        for (int i = buffer.Count - 1; i >= 0; i--) _mainStack.Push(buffer[i]);
                        RefreshMainBackdrop();
                        return;
                    }

                    buffer.Add(top);
                }

                // restore all
                for (int i = buffer.Count - 1; i >= 0; i--) _mainStack.Push(buffer[i]);
            }

            // 2) active main page
            if (_activeMainPage != null && !_activeMainPage.closed && _activeMainPage.view is T)
            {
                CloseEntryAsync(_activeMainPage).Forget();
                _activeMainPage = null;
                RefreshMainBackdrop();
                return;
            }

            // 3) global popup stack (non-main)
            foreach (var e in _globalPopupStack)
            {
                if (e == null || e.closed) continue;
                if (e.view is T)
                {
                    _entriesByLayer[e.layer].Remove(e);
                    CloseEntryAsync(e).Forget();
                    return;
                }
            }

            Debug.LogWarning($"[UI] Close<{typeof(T).Name}> not found.");
        }

        /// <summary>
        /// Main "X" behavior: close top stackable first, then close page.
        /// </summary>
        public void CloseTopMain()
        {
            // close stackable first
            while (_mainStack.Count > 0)
            {
                var top = _mainStack.Pop();
                if (top == null || top.closed) continue;

                CloseEntryAsync(top).Forget();
                RefreshMainBackdrop();
                return;
            }

            // then close page
            if (_activeMainPage != null && !_activeMainPage.closed)
            {
                var page = _activeMainPage;
                _activeMainPage = null;
                CloseEntryAsync(page).Forget();
                RefreshMainBackdrop();
            }
        }

        /// <summary>
        /// Close most recent non-main popup globally.
        /// </summary>
        public void CloseTopPopup()
        {
            while (_globalPopupStack.Count > 0)
            {
                var e = _globalPopupStack.Pop();
                if (e == null || e.closed) continue;

                _entriesByLayer[e.layer].Remove(e);
                CloseEntryAsync(e).Forget();
                return;
            }
        }

        #endregion

        #region Open

        public async UniTask<T> OpenPopupAsync<T>(object args = null, bool withBackdrop = true) where T : UIView
        {
            var key = typeof(T).Name;

            // dedupe concurrent opens
            if (_openingTasks.TryGetValue(key, out var existing))
                return (T)await existing.Task;

            var tcs = new UniTaskCompletionSource<UIView>();
            _openingTasks[key] = tcs;

            try
            {
                // 1) reuse cached
                if (_cachedEntries.TryGetValue(key, out var cached) && cached != null && cached.view != null)
                {
                    await ShowEntryAsync(cached, args, withBackdrop);
                    tcs.TrySetResult(cached.view);
                    return (T)cached.view;
                }

                // 2) load prefab
                var handle = Addressables.LoadAssetAsync<GameObject>(key);
                await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    tcs.TrySetResult(null);
                    return null;
                }

                var prefab = handle.Result;
                var prefabView = prefab.GetComponent<UIView>();
                if (prefabView == null)
                {
                    Addressables.Release(handle);
                    tcs.TrySetResult(null);
                    return null;
                }

                var layer = prefabView.Layer;
                var parent = GetLayerRoot(layer);
                if (parent == null)
                {
                    Addressables.Release(handle);
                    tcs.TrySetResult(null);
                    return null;
                }

                Debug.Log("OpenPopupAsync: " + prefab.name);
                var go = Instantiate(prefab, parent, false);
                var typed = go.GetComponent<T>();
                if (typed == null)
                {
                    Destroy(go);
                    Addressables.Release(handle);
                    tcs.TrySetResult(null);
                    return null;
                }

                var entry = new Entry
                {
                    key = key,
                    layer = layer,
                    handle = handle,
                    view = typed,
                    closed = false,
                    cacheOnClose = typed.CacheOnClose || layer == UILayer.Main // main always cache by design
                };

                // MAIN layer special
                if (layer == UILayer.Main)
                {
                    // enforce caching on main
                    entry.cacheOnClose = true;
                    typed.CacheOnClose = true;

                    await HandleMainOpenAsync(entry, args, withBackdrop);

                    // cache main
                    _cachedEntries[key] = entry;

                    tcs.TrySetResult(typed);
                    return typed;
                }

                // NORMAL popup logic (non-main)
                if (withBackdrop)
                    CreatePerEntryBackdrop(entry);

                _entriesByLayer[layer].Add(entry);
                _globalPopupStack.Push(entry);

                await typed.PlayShowAsync(args);

                if (entry.cacheOnClose)
                    _cachedEntries[key] = entry;

                tcs.TrySetResult(typed);
                return typed;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                tcs.TrySetResult(null);
                return null;
            }
            finally
            {
                _openingTasks.Remove(key);
            }
        }

        private async UniTask ShowEntryAsync(Entry entry, object args, bool withBackdrop)
        {
            if (entry == null || entry.view == null) return;

            // already visible
            if (!entry.closed && entry.view.gameObject.activeSelf)
                return;

            entry.closed = false;
            entry.view.gameObject.SetActive(true);

            // main
            if (entry.layer == UILayer.Main)
            {
                await HandleMainOpenAsync(entry, args, withBackdrop);
                _cachedEntries[entry.key] = entry; // keep cached
                return;
            }

            // non-main
            if (withBackdrop)
                CreatePerEntryBackdrop(entry);

            _entriesByLayer[entry.layer].Add(entry);
            _globalPopupStack.Push(entry);

            await entry.view.PlayShowAsync(args);

            if (entry.cacheOnClose)
                _cachedEntries[entry.key] = entry;
        }

        #endregion

        #region Main Layer Core (Page + Stack + Shared Backdrop)

        private void EnsureMainBackdrop()
        {
            if (backdropPrefab == null) return;

            var mainRoot = GetLayerRoot(UILayer.Main);
            if (mainRoot == null) return;

            if (_mainSharedBackdrop == null)
            {
                _mainSharedBackdrop = Instantiate(backdropPrefab, mainRoot, false);

                // optional: click backdrop closes top main (like X)
                var btn = _mainSharedBackdrop.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        var top = PeekTopMainView();
                        if (top != null && top.CloseOnBackdrop)
                            CloseTopMain();
                    });
                }
            }

            _mainSharedBackdrop.SetActive(true);
        }

        private void RefreshMainBackdrop()
        {
            if (_mainSharedBackdrop == null) return;
            _mainSharedBackdrop.SetActive(IsAnyMainVisible());
        }

        private UIView PeekTopMainView()
        {
            // stack top first
            foreach (var e in _mainStack)
            {
                if (e == null || e.closed || e.view == null) continue;
                if (e.view.gameObject.activeSelf) return e.view;
            }

            if (_activeMainPage != null && !_activeMainPage.closed && _activeMainPage.view != null &&
                _activeMainPage.view.gameObject.activeSelf)
                return _activeMainPage.view;

            return null;
        }

        private async UniTask CloseAllMainStackAsync()
        {
            while (_mainStack.Count > 0)
            {
                var e = _mainStack.Pop();
                if (e == null || e.closed) continue;

                await CloseEntryAsync(e);
            }
        }

        private async UniTask HandleMainOpenAsync(Entry entry, object args, bool withBackdrop)
        {
            if (withBackdrop)
            {
                EnsureMainBackdrop();
            }

            // ensure under correct parent (in case cached instance)
            var mainRoot = GetLayerRoot(UILayer.Main);
            entry.view.transform.SetParent(mainRoot, false);
            entry.view.gameObject.SetActive(true);

            if (entry.view.MainMode == MainLayerMode.PageExclusive)
            {
                // RULE B: open page => close all stackables first
                await CloseAllMainStackAsync();

                // close current page (if different)
                if (_activeMainPage != null && !_activeMainPage.closed && _activeMainPage != entry)
                    await CloseEntryAsync(_activeMainPage);

                _activeMainPage = entry;

                // order: backdrop under page
                if (withBackdrop) if (_mainSharedBackdrop != null) _mainSharedBackdrop.transform.SetAsLastSibling();
                entry.view.transform.SetAsLastSibling();
            }
            else // Stackable
            {
                _mainStack.Push(entry);

                // order: backdrop -> page -> stackable
                if (_mainSharedBackdrop != null) _mainSharedBackdrop.transform.SetAsLastSibling();

                if (_activeMainPage != null && !_activeMainPage.closed && _activeMainPage.view != null &&
                    _activeMainPage.view.gameObject.activeSelf)
                    _activeMainPage.view.transform.SetAsLastSibling();

                entry.view.transform.SetAsLastSibling();
            }

            await entry.view.PlayShowAsync(args);
            
            if (withBackdrop)
            {
                RefreshMainBackdrop();
            }
        }

        #endregion

        #region Backdrop (non-main per entry)

        private void CreatePerEntryBackdrop(Entry entry)
        {
            if (backdropPrefab == null) return;

            // avoid duplicate
            if (entry.backdropInstance != null)
            {
                Destroy(entry.backdropInstance);
                entry.backdropInstance = null;
            }

            var parent = GetLayerRoot(entry.layer);
            entry.backdropInstance = Instantiate(backdropPrefab, parent, false);
            entry.backdropInstance.transform.SetAsFirstSibling();
            var btn = entry.backdropInstance.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    var top = PeekTopPopupView();
                    if (top != null && top.CloseOnBackdrop)
                        CloseTopPopup();
                });
            }
        }

        private UIView PeekTopPopupView()
        {
            foreach (var e in _globalPopupStack)
            {
                if (e == null || e.closed || e.view == null) continue;
                if (e.view.gameObject.activeSelf) return e.view;
            }

            return null;
        }

        #endregion

        #region Close / Cleanup

        private async UniTask CloseEntryAsync(Entry entry)
        {
            if (entry == null || entry.closed) return;

            entry.closed = true;

            if (entry.view != null)
            {
                await entry.view.PlayHideAsync();

                if (entry.cacheOnClose)
                {
                    entry.view.gameObject.SetActive(false);
                }
                else
                {
                    Destroy(entry.view.gameObject);
                    entry.view = null;
                }
            }

            // main: shared backdrop is handled by RefreshMainBackdrop()
            if (entry.layer == UILayer.Main)
            {
                // remove main references if needed
                if (_activeMainPage == entry) _activeMainPage = null;
                // (stack entries are popped by CloseTopMain / CloseAllMainStackAsync)
                //RefreshMainBackdrop();
                return;
            }

            // non-main: destroy per-entry backdrop
            if (entry.backdropInstance != null)
            {
                Destroy(entry.backdropInstance);
                entry.backdropInstance = null;
            }

            // release addressables handle if not caching
            if (!entry.cacheOnClose && entry.handle.IsValid())
                Addressables.Release(entry.handle);
        }

        #endregion

        #region Main Scene Init

        // Put BottomMainView on SubMain, TopMainView on OverMain (NOT Main)
        public async UniTask InitMainScene()
        {
            var result = await UniTask.WhenAll(
                OpenPopupAsync<BottomMainView>(withBackdrop: false),
                OpenPopupAsync<TopMainView>(withBackdrop: false)
            );

            tapeAnimator.transform.parent = GetLayerRoot(UILayer.Main);
        }

        private void OnInitSceneDataComplete()
        {
            loadingSceneCanvasGroup.DOFade(0f, 0.5f).SetEase(Ease.OutCubic);
            loadingSceneCanvasGroup.blocksRaycasts = false;
            loadingSceneCanvasGroup.interactable = false;
        }

        #endregion
    }
}