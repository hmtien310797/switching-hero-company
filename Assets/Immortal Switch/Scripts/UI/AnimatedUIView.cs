using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Immortal_Switch.Scripts.UI
{
    public abstract class AnimatedUIView : UIView
    {
        [Header("Animation")]
        [SerializeField] protected RectTransform animatedRoot; // root panel cần bay
        [SerializeField] protected float showDuration = 0.25f;
        [SerializeField] protected float hideDuration = 0.20f;

        [Tooltip("Vị trí bắt đầu khi mở = anchoredPosition + offset")]
        [SerializeField] protected Vector2 showFromOffset = new Vector2(0, 900);

        [Tooltip("Vị trí kết thúc khi đóng = anchoredPosition + offset")]
        [SerializeField] protected Vector2 hideToOffset = new Vector2(0, 900);

        [SerializeField] protected bool fade = true;
        [SerializeField] protected float fromAlpha = 0f;
        [SerializeField] protected float toAlpha = 1f;

        [SerializeField] protected AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private CanvasGroup _cg;
        private Vector2 _basePos;
        private bool _inited;

        protected virtual void EnsureInit()
        {
            if (_inited) return;

            if (animatedRoot == null)
                animatedRoot = transform as RectTransform;

            _basePos = animatedRoot.anchoredPosition;

            if (fade)
            {
                _cg = GetComponent<CanvasGroup>();
                if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
            }

            _inited = true;
            animatedRoot.gameObject.SetActive(false);
        }

        public override void OnShow(object args) { /* logic data-binding nếu cần */ }
        public override void OnHide() { /* cleanup nếu cần */ }

        public override async UniTask PlayShowAsync(object args)
        {
            EnsureInit();

            // gọi OnShow trước để setup data, rồi animate
            OnShow(args);

            var startPos = _basePos + showFromOffset;
            var endPos = _basePos;

            animatedRoot.anchoredPosition = startPos;
            if (fade && _cg != null) _cg.alpha = fromAlpha;
            animatedRoot.gameObject.SetActive(true);

            await Tween01(showDuration, t =>
            {
                float e = ease.Evaluate(t);
                animatedRoot.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, e);
                if (fade && _cg != null) _cg.alpha = Mathf.LerpUnclamped(fromAlpha, toAlpha, e);
            });
        }

        public override async UniTask PlayHideAsync()
        {
            EnsureInit();

            var startPos = animatedRoot.anchoredPosition;
            var endPos = _basePos + hideToOffset;

            float startAlpha = (fade && _cg != null) ? _cg.alpha : 1f;

            await Tween01(hideDuration, t =>
            {
                float e = ease.Evaluate(t);
                animatedRoot.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, e);
                if (fade && _cg != null) _cg.alpha = Mathf.LerpUnclamped(startAlpha, fromAlpha, e);
            });

            OnHide();
        }

        protected static async UniTask Tween01(float duration, System.Action<float> onUpdate)
        {
            if (duration <= 0f)
            {
                onUpdate?.Invoke(1f);
                return;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                onUpdate?.Invoke(p);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            onUpdate?.Invoke(1f);
        }
    }
}