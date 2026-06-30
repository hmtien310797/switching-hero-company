using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonBounce : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    [SerializeField] private RectTransform target;
    [SerializeField] private float pressedScale = 0.9f;
    [SerializeField] private float bounceScale = 1.1f;
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private float bounceDuration = 0.15f;

    private Tween scaleTween;

    private void Awake()
    {
        if (target == null)
            target = transform as RectTransform;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        scaleTween?.Kill();

        scaleTween = target.DOScale(pressedScale, pressDuration)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        scaleTween?.Kill();

        Sequence seq = DOTween.Sequence();

        seq.Append(
            target.DOScale(bounceScale, bounceDuration * 0.5f)
                .SetEase(Ease.OutQuad));

        seq.Append(
            target.DOScale(1f, bounceDuration * 0.5f)
                .SetEase(Ease.OutBack));

        scaleTween = seq;
    }

    private void OnDisable()
    {
        scaleTween?.Kill();
        target.localScale = Vector3.one;
    }
}