using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Immortal_Switch.Scripts.UI
{
    /// <summary>
    /// Base class cho UI popup có hiệu ứng bounce khi hiện và tắt.
    /// Khi hiện: scale từ 0 → bounceScale → 1 (phóng to nhẹ rồi thu về kích cỡ ban đầu).
    /// Khi tắt: scale từ 1 → bounceScale → 0 (phóng to nhẹ rồi thu nhỏ về 0).
    /// </summary>
    public abstract class BouncePopupUIView : UIView
    {
        [Header("Bounce Animation")]
        [SerializeField]
        protected RectTransform animatedRoot;

        [SerializeField]
        [Min(0f)]
        protected float showDuration = 0.35f;

        [SerializeField]
        [Min(0f)]
        protected float hideDuration = 0.25f;

        [Tooltip("Hệ số scale vượt quá (overshoot) khi bounce. VD: 1.08 nghĩa là phóng to 108% rồi thu về.")]
        [SerializeField]
        [Range(1f, 1.3f)]
        protected float bounceScale = 1.08f;

        [Tooltip("Ease cho giai đoạn scale-up (show: 0 → bounceScale, hide: 1 → bounceScale)")]
        [SerializeField]
        protected Ease showEase = Ease.OutBack;

        [Tooltip("Ease cho giai đoạn scale-down (hide: bounceScale → 0)")]
        [SerializeField]
        protected Ease hideEase = Ease.InBack;

        [Header("Fade")]
        [SerializeField]
        protected bool fade = true;

        [SerializeField]
        [Min(0f)]
        protected float fadeDuration = 0.2f;

        private CanvasGroup _canvasGroup;
        private Vector3 _baseScale;
        private bool _initialized;
        private Sequence _animationSequence;

        protected virtual void EnsureInit()
        {
            if (_initialized)
                return;

            if (animatedRoot == null)
                animatedRoot = transform as RectTransform;

            _baseScale = animatedRoot.localScale;

            if (fade)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _initialized = true;
            animatedRoot.gameObject.SetActive(false);
        }

        public override void OnShow(object args) { /* data-binding logic */ }
        public override void OnHide() { /* cleanup logic */ }

        public override async UniTask PlayShowAsync(object args)
        {
            EnsureInit();
            KillAnimation();

            // Gọi OnShow trước để setup data
            OnShow(args);

            // Bắt đầu từ scale 0
            animatedRoot.localScale = Vector3.zero;
            if (fade && _canvasGroup != null)
                _canvasGroup.alpha = 0f;

            animatedRoot.gameObject.SetActive(true);

            var tcs = new UniTaskCompletionSource();

            _animationSequence = DOTween.Sequence()
                .SetUpdate(true);

            // Giai đoạn 1: 0 → bounceScale (phóng to vượt kích cỡ ban đầu)
            _animationSequence.Append(
                animatedRoot
                    .DOScale(_baseScale * bounceScale, showDuration * 0.65f)
                    .SetEase(showEase)
            );

            // Giai đoạn 2: bounceScale → 1 (thu về kích cỡ ban đầu)
            _animationSequence.Append(
                animatedRoot
                    .DOScale(_baseScale, showDuration * 0.35f)
                    .SetEase(Ease.OutCubic)
            );

            // Fade in song song
            if (fade && _canvasGroup != null)
            {
                _animationSequence.Join(
                    _canvasGroup
                        .DOFade(1f, fadeDuration)
                        .SetEase(Ease.OutCubic)
                );
            }

            _animationSequence.OnComplete(() => tcs.TrySetResult());

            await tcs.Task;
        }

        public override async UniTask PlayHideAsync()
        {
            EnsureInit();
            KillAnimation();

            var tcs = new UniTaskCompletionSource();

            _animationSequence = DOTween.Sequence()
                .SetUpdate(true);

            // Giai đoạn 1: 1 → bounceScale (phóng to nhẹ)
            _animationSequence.Append(
                animatedRoot
                    .DOScale(_baseScale * bounceScale, hideDuration * 0.3f)
                    .SetEase(Ease.OutCubic)
            );

            // Giai đoạn 2: bounceScale → 0 (thu nhỏ về 0)
            _animationSequence.Append(
                animatedRoot
                    .DOScale(Vector3.zero, hideDuration * 0.7f)
                    .SetEase(hideEase)
            );

            // Fade out song song
            if (fade && _canvasGroup != null)
            {
                _animationSequence.Join(
                    _canvasGroup
                        .DOFade(0f, hideDuration)
                        .SetEase(Ease.InCubic)
                );
            }

            _animationSequence.OnComplete(() =>
            {
                OnHide();
                tcs.TrySetResult();
            });

            await tcs.Task;
        }

        /// <summary>
        /// Huỷ animation DOTween hiện tại (nếu có).
        /// Gọi khi cần dừng animation giữa chừng (VD: mở lại popup khi đang đóng).
        /// </summary>
        protected void KillAnimation()
        {
            if (_animationSequence != null)
            {
                _animationSequence.Kill(false);
                _animationSequence = null;
            }

            if (animatedRoot != null)
                animatedRoot.DOKill(false);

            if (_canvasGroup != null)
                _canvasGroup.DOKill(false);
        }

        protected virtual void OnDestroy()
        {
            KillAnimation();
        }
    }
}
