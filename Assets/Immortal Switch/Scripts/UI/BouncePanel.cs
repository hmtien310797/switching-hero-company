using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Base class độc lập cho panel có hiệu ứng bounce khi bật/tắt.
/// KHÔNG phụ thuộc vào UIManager — hoạt động như một MonoBehaviour bình thường.
///
/// Show: scale từ 0 → bounceScale → 1 (phóng to nhẹ rồi thu về kích cỡ ban đầu).
/// Hide: scale từ 1 → bounceScale → 0 (phóng to nhẹ rồi thu nhỏ về 0).
///
/// Lifecycle:
///   Show()       → OnShow() → (animation) → OnShowCompletely()
///   Hide()       → OnHide() → (animation) → OnHideCompletely()
/// 
/// Cách dùng: Kế thừa BouncePanel, gọi Show() / Hide() khi cần.
/// </summary>
public abstract class BouncePanel : MonoBehaviour
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

    [Tooltip("Hệ số scale vượt quá (overshoot) khi bounce. VD: 1.08 = 108%.")]
    [SerializeField]
    [Range(1f, 1.3f)]
    protected float bounceScale = 1.08f;

    [Tooltip("Ease cho giai đoạn scale-up (show: 0 → bounceScale, hide: 1 → bounceScale).")]
    [SerializeField]
    protected Ease showEase = Ease.OutBack;

    [Tooltip("Ease cho giai đoạn scale-down (hide: bounceScale → 0).")]
    [SerializeField]
    protected Ease hideEase = Ease.InBack;

    [Header("Fade")]
    [SerializeField]
    protected bool fade = true;

    [SerializeField]
    [Min(0f)]
    protected float fadeDuration = 0.2f;

    // ---- Internal state ----
    private CanvasGroup _canvasGroup;
    private Vector3 _baseScale;
    private bool _initialized;
    private Sequence _animationSequence;

    /// <summary>Panel đang trong quá trình show animation.</summary>
    public bool IsShowing { get; private set; }

    /// <summary>Panel đang trong quá trình hide animation.</summary>
    public bool IsHiding { get; private set; }

    /// <summary>Panel đang thực sự hiển thị (đã show xong và chưa hide).</summary>
    public bool IsVisible { get; private set; }

    // ============================================================
    // PUBLIC API
    // ============================================================

    /// <summary>
    /// Hiện panel với animation bounce kèm callback khi animation hoàn tất.
    /// </summary>
    public void Show(Action onComplete = null)
    {
        EnsureInit();
        KillAnimation();

        // Nếu đang visible và không đang hide thì bỏ qua
        bool isActuallyVisible =
            gameObject.activeInHierarchy &&
            animatedRoot.localScale.sqrMagnitude > 0.0001f &&
            (!fade || _canvasGroup == null || _canvasGroup.alpha > 0.001f);

        if (IsVisible && !IsHiding && isActuallyVisible)
        {
            onComplete?.Invoke();
            return;
        }

        IsShowing = true;
        IsHiding = false;

        // Đảm bảo GameObject active trước khi animate
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        // Reset trạng thái ban đầu
        animatedRoot.localScale = Vector3.zero;
        if (fade && _canvasGroup != null)
            _canvasGroup.alpha = 0f;

        // Callback trước animation
        OnShow();

        _animationSequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetAutoKill(true);

        // Giai đoạn 1: 0 → bounceScale (phóng to vượt kích cỡ ban đầu)
        _animationSequence.Append(
            animatedRoot
                .DOScale(1f * bounceScale, showDuration * 0.65f)
                .SetEase(showEase)
        );

        // Giai đoạn 2: bounceScale → 1 (thu về kích cỡ ban đầu)
        _animationSequence.Append(
            animatedRoot
                .DOScale(1f, showDuration * 0.35f)
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

        _animationSequence.OnComplete(() =>
        {
            _animationSequence = null;
            IsShowing = false;
            IsVisible = true;
            OnShowCompletely();
            onComplete?.Invoke();
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        });
    }

    /// <summary>
    /// Ẩn panel với animation bounce kèm callback khi animation hoàn tất.
    /// </summary>
    public void Hide(Action onComplete = null)
    {
        EnsureInit();
        KillAnimation();

        // Nếu đang ẩn rồi thì gọi thẳng callback
        if (!IsVisible && !IsShowing)
        {
            onComplete?.Invoke();
            return;
        }

        IsHiding = true;
        IsShowing = false;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        // Callback trước animation
        OnHide();
        _animationSequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetAutoKill(true);

        // Giai đoạn 1: current → bounceScale (phóng to nhẹ)
        _animationSequence.Append(
            animatedRoot
                .DOScale(1f * bounceScale, hideDuration * 0.3f)
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
            _animationSequence = null;
            IsHiding = false;
            IsVisible = false;

            // Chỉ SetActive(false) nếu object vẫn đang active trong hierarchy
            if (gameObject.activeInHierarchy)
                gameObject.SetActive(false);

            OnHideCompletely();
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// Hiện/ẩn ngay lập tức không có animation.
    /// </summary>
    public void SetVisibleImmediate(bool visible)
    {
        EnsureInit();
        KillAnimation();

        IsShowing = false;
        IsHiding = false;
        IsVisible = visible;

        if (visible)
        {
            gameObject.SetActive(true);

            animatedRoot.localScale = _baseScale;

            if (fade && _canvasGroup != null)
                _canvasGroup.alpha = 1f;

            OnShow();
            OnShowCompletely();
        }
        else
        {
            OnHide();

            animatedRoot.localScale = Vector3.zero;

            if (fade && _canvasGroup != null)
                _canvasGroup.alpha = 0f;

            gameObject.SetActive(false);

            OnHideCompletely();
        }
    }

    // ============================================================
    // LIFECYCLE HOOKS (override để dùng)
    // ============================================================

    /// <summary>Gọi TRƯỚC khi animation show bắt đầu. Dùng để setup data.</summary>
    protected virtual void OnShow() { }

    /// <summary>Gọi SAU khi animation show kết thúc.</summary>
    protected virtual void OnShowCompletely() { }

    /// <summary>Gọi TRƯỚC khi animation hide bắt đầu. Dùng để cleanup.</summary>
    protected virtual void OnHide() { }

    /// <summary>Gọi SAU khi animation hide kết thúc (panel đã SetActive(false)).</summary>
    protected virtual void OnHideCompletely() { }

    // ============================================================
    // INTERNAL
    // ============================================================

    private void EnsureInit()
    {
        if (_initialized)
            return;

        if (animatedRoot == null)
            animatedRoot = transform as RectTransform;

        _baseScale = Vector3.one;

        if (fade)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        _initialized = true;

        // Ẩn panel khi khởi tạo
        animatedRoot.localScale = Vector3.zero;
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void KillAnimation()
    {
        // Chỉ kill nếu sequence còn active (chưa auto-kill)
        if (_animationSequence != null)
        {
            if (_animationSequence.IsActive())
                _animationSequence.Kill(false);
            _animationSequence = null;
        }

        // Kill các tween còn sót trên animatedRoot và canvasGroup
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
