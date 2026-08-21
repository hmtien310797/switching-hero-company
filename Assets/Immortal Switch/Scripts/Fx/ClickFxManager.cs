using Coffee.UIExtensions;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Fx
{
    /// <summary>
    /// Spawns pooled <see cref="UIParticle"/> effects at a screen position on a dedicated,
    /// top-most **sub-canvas** that:
    ///   - does NOT block raycast (no GraphicRaycaster, UIParticle.raycastTarget = false),
    ///   - renders above every UILayer root (high sortingOrder + last sibling),
    ///   - is a nested canvas, so spawning particles never triggers the main canvas rebuild.
    ///
    /// The FX canvas is created automatically as a child of the scene's root Canvas and inherits
    /// its render mode / camera / scaler, so no per-scene setup is required.
    /// </summary>
    public sealed class ClickFxManager : Singleton<ClickFxManager>
    {
        [Header("Prefab")]
        [Tooltip("Optional UIParticle effect prefab (must have a ClickFxObject + UIParticle + ParticleSystem). " +
                 "If empty, a default burst effect is built at runtime.")]
        [SerializeField] private ClickFxObject fxPrefab;

        [Header("FX Canvas")]
        [Tooltip("sortingOrder of the FX sub-canvas. Must be above the UILayer.System roots.")]
        [SerializeField] private int sortingOrder = 1000;

        [Header("Input")]
        [Tooltip("When true, the manager itself listens for raw pointer presses and spawns an effect "
                 + "on every click/tap. Disable it if you want to control spawning manually or via a "
                 + "scoped ScreenClickFxListener on a specific screen.")]
        [SerializeField] private bool listenForClicks = true;

        [Header("Debug")]
        [Tooltip("Logs each click's screen position and the resulting world position (for diagnosing offsets).")]
        [SerializeField] private bool logClicks;

        private RectTransform _fxRoot;
        private Canvas _fxCanvas;
        private Canvas _rootCanvas;
        private ClickFxObject _runtimeFxPrefab;

        protected override void OnSingletonAwake()
        {
            EnsureFxCanvas();
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        private void Update()
        {
            if (!listenForClicks)
                return;

            // If a scoped ScreenClickFxListener exists, let it drive spawning (avoid double spawn).
            if (ScreenClickFxListener.ActiveCount > 0)
                return;

            if (ScreenClickFxListener.TryGetPointerPress(out var screenPos))
                Play(screenPos);
        }

        /// <summary>
        /// Spawn an effect at a screen-space position (e.g. Input.mousePosition / touch position).
        /// Converts the point into a world point on the FX canvas plane and plays a pooled effect there.
        /// </summary>
        public void Play(Vector2 screenPosition)
        {
            if (_fxRoot == null)
                EnsureFxCanvas();

            if (_fxRoot == null)
                return;

            // Use the ROOT canvas' camera (a nested canvas' worldCamera can read null even when the
            // parent is camera-based). Same convention as HeroJoystick.
            Camera uiCamera = _rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _rootCanvas.worldCamera
                : null;

            // Convert to a world point on the FX canvas plane, then place the effect there directly.
            // Placing by world position is robust against nested-canvas coordinate quirks.
            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    _fxRoot,
                    screenPosition,
                    uiCamera,
                    out Vector3 world))
            {
                if (logClicks)
                    Debug.Log($"[ClickFx] Convert failed for screen {screenPosition} (camera {uiCamera}).");
                return;
            }

            if (logClicks)
                Debug.Log($"[ClickFx] screen={screenPosition} world={world} canvas={_rootCanvas?.name} mode={_rootCanvas?.renderMode} cam={uiCamera}");

            PlayAtWorld(world);
        }

        /// <summary>
        /// Spawn an effect at a world-space point on the FX canvas plane.
        /// </summary>
        public void PlayAtWorld(Vector3 worldPosition)
        {
            ClickFxObject template = fxPrefab != null ? fxPrefab : EnsureRuntimeFxPrefab();
            if (template == null)
                return;

            ClickFxObject fx = PoolManager.Instance.Spawn(template, Vector3.zero, Quaternion.identity, _fxRoot);
            if (fx == null)
                return;

            fx.SetWorldPosition(worldPosition);
        }

        [ContextMenu("Play At Screen Center")]
        private void DebugPlayScreenCenter()
        {
            Play(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        }

        // ===== FX canvas setup =====

        private void EnsureFxCanvas()
        {
            if (_fxRoot != null)
                return;

            Canvas mainCanvas = FindRootCanvas();
            if (mainCanvas == null)
            {
                Debug.LogError("[ClickFxManager] No root Canvas found. Add a canvas (or UIManager) to the scene first.");
                return;
            }

            _rootCanvas = mainCanvas;

            var go = new GameObject("ClickFxCanvas", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(mainCanvas.transform, false);

            _fxCanvas = go.GetComponent<Canvas>();
            _fxCanvas.overrideSorting = true;
            go.layer = LayerMask.NameToLayer("UI");
            _fxCanvas.sortingOrder = sortingOrder;

            // Nested canvas: a separate batch group, so the main canvas does NOT rebuild when
            // particles spawn/despawn. It inherits the parent canvas' render mode + camera + scale
            // (no CanvasScaler here — a nested scaler would double-scale against the root).
            // NOTE: intentionally NO GraphicRaycaster => clicks pass through to lower canvases.

            _fxRoot = go.GetComponent<RectTransform>();
            StretchFull(_fxRoot);

            _fxRoot.SetAsLastSibling(); // render above all UILayer roots (Background..System)
        }

        private static Canvas FindRootCanvas()
        {
            // Prefer the canvas owned by UIManager (the scene's main UI canvas).
            if (UIManager.Instance != null)
            {
                var uiCanvas = UIManager.Instance.GetComponent<Canvas>();
                if (uiCanvas != null && !HasAncestorCanvas(uiCanvas.transform))
                    return uiCanvas;
            }

            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (!HasAncestorCanvas(canvas.transform))
                    return canvas;
            }

            return null;
        }

        private static bool HasAncestorCanvas(Transform t)
        {
            var parent = t.parent;
            while (parent != null)
            {
                if (parent.GetComponent<Canvas>() != null)
                    return true;
                parent = parent.parent;
            }

            return false;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
        }

        // ===== Default runtime prefab =====

        private ClickFxObject EnsureRuntimeFxPrefab()
        {
            if (_runtimeFxPrefab != null)
                return _runtimeFxPrefab;

            _runtimeFxPrefab = BuildDefaultFx();
            return _runtimeFxPrefab;
        }

        /// <summary>Build a minimal soft-burst effect so the feature works with zero asset setup.</summary>
        private ClickFxObject BuildDefaultFx()
        {
            var root = new GameObject("ClickFx_Default", typeof(RectTransform));
            root.transform.SetParent(transform, false);

            var uiParticle = root.AddComponent<UIParticle>();
            uiParticle.raycastTarget = false;

            var psGo = new GameObject("Burst", typeof(RectTransform));
            psGo.transform.SetParent(root.transform, false);
            var ps = psGo.AddComponent<ParticleSystem>();
            ConfigureSoftBurst(ps);

            var fx = root.AddComponent<ClickFxObject>();
            fx.AssignParticle(uiParticle);

            // Template stays inactive; pooled clones are activated on spawn.
            root.SetActive(false);

            return fx;
        }

        private static void ConfigureSoftBurst(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 0.3f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(12f, 28f);
            main.startSize = new ParticleSystem.MinMaxCurve(30f, 55f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.6f, 1f));
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(1f, 1f)));

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
    }
}