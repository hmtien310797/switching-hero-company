using DG.Tweening;
using UnityEngine;

public class UILocalRotator : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private float duration = 1f;
    [SerializeField] private bool clockwise = true;
    [SerializeField] private bool resetRotationOnDisable = true;

    private Tween rotateTween;
    private Quaternion initialLocalRotation;

    private void Awake()
    {
        if (target == null)
            target = transform as RectTransform;

        if (target != null)
            initialLocalRotation = target.localRotation;
    }

    private void OnEnable()
    {
        StartRotate();
    }

    private void OnDisable()
    {
        StopRotate();

        if (resetRotationOnDisable && target != null)
            target.localRotation = initialLocalRotation;
    }

    private void StartRotate()
    {
        if (target == null)
            return;

        StopRotate();

        float angle = clockwise ? -360f : 360f;

        rotateTween = target
            .DOLocalRotate(
                new Vector3(0f, 0f, angle),
                duration,
                RotateMode.FastBeyond360)
            .SetRelative()
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .SetUpdate(true);
    }

    private void StopRotate()
    {
        rotateTween?.Kill();
        rotateTween = null;
    }
}