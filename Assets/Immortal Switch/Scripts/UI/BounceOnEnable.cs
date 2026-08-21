using System.Collections;
using UnityEngine;

public class BounceOnEnable : MonoBehaviour
{
    [Header("Bounce Settings")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float startScale = 0.7f;
    [SerializeField] private float overshootScale = 1.2f;

    private Vector3 originalScale;
    private Coroutine bounceCoroutine;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (bounceCoroutine != null)
            StopCoroutine(bounceCoroutine);

        bounceCoroutine = StartCoroutine(Bounce());
    }

    private IEnumerator Bounce()
    {
        Vector3 start = originalScale * startScale;
        Vector3 overshoot = originalScale * overshootScale;

        transform.localScale = start;

        // Scale lên quá mức một chút
        float halfDuration = duration * 0.6f;
        float time = 0f;

        while (time < halfDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / halfDuration);

            // Ease out
            t = 1f - Mathf.Pow(1f - t, 3f);

            transform.localScale = Vector3.LerpUnclamped(start, overshoot, t);
            yield return null;
        }

        // Scale trở về kích thước gốc
        float returnDuration = duration - halfDuration;
        time = 0f;

        while (time < returnDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / returnDuration);

            // Ease out
            t = 1f - Mathf.Pow(1f - t, 3f);

            transform.localScale = Vector3.LerpUnclamped(overshoot, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
        bounceCoroutine = null;
    }

    private void OnDisable()
    {
        transform.localScale = originalScale;
    }
}