using DG.Tweening;
using UnityEngine;

public class UIFloatingObject : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private RectTransform target;

    [Header("Floating")]
    [SerializeField] private float moveDistance = 15f;
    [SerializeField] private float moveDuration = 0.8f;
    [SerializeField] private Ease ease = Ease.InOutSine;

    private Tween _floatingTween;
    private Vector2 _initialAnchoredPosition;
    private bool _hasCachedInitialPosition;

    private void Awake()
    {
        if (target == null)
        {
            target = transform as RectTransform;
        }

        CacheInitialPosition();
    }

    private void OnEnable()
    {
        if (target == null)
        {
            return;
        }

        CacheInitialPosition();

        // Đảm bảo object luôn bắt đầu đúng vị trí gốc.
        target.anchoredPosition = _initialAnchoredPosition;

        KillTween();

        _floatingTween = target
            .DOAnchorPosY(
                _initialAnchoredPosition.y + moveDistance,
                moveDuration
            )
            .SetEase(ease)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }

    private void OnDisable()
    {
        KillTween();
        RestoreInitialPosition();
    }

    private void OnDestroy()
    {
        KillTween();
    }

    private void CacheInitialPosition()
    {
        if (target == null || _hasCachedInitialPosition)
        {
            return;
        }

        _initialAnchoredPosition = target.anchoredPosition;
        _hasCachedInitialPosition = true;
    }

    private void RestoreInitialPosition()
    {
        if (target == null || !_hasCachedInitialPosition)
        {
            return;
        }

        target.anchoredPosition = _initialAnchoredPosition;
    }

    private void KillTween()
    {
        if (_floatingTween == null)
        {
            return;
        }

        _floatingTween.Kill();
        _floatingTween = null;
    }
}