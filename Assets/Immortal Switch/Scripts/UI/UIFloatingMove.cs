using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIFloatingMove : MonoBehaviour
{
    [SerializeField] private float moveDistance = 15f;
    [SerializeField] private float duration = 0.7f;
    [SerializeField] private Ease ease = Ease.InOutSine;

    private RectTransform rectTransform;
    private Vector2 startPosition;
    private Tween moveTween;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        rectTransform.anchoredPosition = startPosition;

        moveTween?.Kill();

        moveTween = rectTransform
            .DOAnchorPosY(startPosition.y + moveDistance, duration)
            .SetEase(ease)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDisable()
    {
        moveTween?.Kill();
        moveTween = null;

        rectTransform.anchoredPosition = startPosition;
    }
}