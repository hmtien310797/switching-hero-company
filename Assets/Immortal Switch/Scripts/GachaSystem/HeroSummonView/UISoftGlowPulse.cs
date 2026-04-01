using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class UISoftGlowPulse : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;

        [Header("Alpha")]
        [SerializeField] private float minAlpha = 0.35f;
        [SerializeField] private float maxAlpha = 0.85f;
        [SerializeField] private float alphaSpeed = 2.2f;

        [Header("Scale")]
        [SerializeField] private float minScale = 0.96f;
        [SerializeField] private float maxScale = 1.06f;
        [SerializeField] private float scaleSpeed = 1.8f;

        [Header("Rotate")]
        [SerializeField] private bool rotate;
        [SerializeField] private float rotateSpeed = 18f;

        [Header("Use Unscaled Time")]
        [SerializeField] private bool useUnscaledTime = true;

        private float seed;

        private void Awake()
        {
            seed = Random.Range(0f, 10f);

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (rectTransform == null)
                rectTransform = transform as RectTransform;
        }

        private void OnEnable()
        {
            seed = Random.Range(0f, 10f);
        }

        private void Update()
        {
            float time = useUnscaledTime ? Time.unscaledTime : Time.time;

            float tAlpha = (Mathf.Sin((time + seed) * alphaSpeed) + 1f) * 0.5f;
            float tScale = (Mathf.Sin((time + seed * 1.37f) * scaleSpeed) + 1f) * 0.5f;

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, tAlpha);

            if (rectTransform != null)
            {
                float scale = Mathf.Lerp(minScale, maxScale, tScale);
                rectTransform.localScale = Vector3.one * scale;

                if (rotate)
                    rectTransform.localRotation = Quaternion.Euler(0f, 0f, time * rotateSpeed);
            }
        }

        public void SetProfile(
            float newMinAlpha,
            float newMaxAlpha,
            float newAlphaSpeed,
            float newMinScale,
            float newMaxScale,
            float newScaleSpeed,
            bool newRotate,
            float newRotateSpeed)
        {
            minAlpha = newMinAlpha;
            maxAlpha = newMaxAlpha;
            alphaSpeed = newAlphaSpeed;
            minScale = newMinScale;
            maxScale = newMaxScale;
            scaleSpeed = newScaleSpeed;
            rotate = newRotate;
            rotateSpeed = newRotateSpeed;
        }

        public void ResetVisual()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = minAlpha;

            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one * minScale;
                rectTransform.localRotation = Quaternion.identity;
            }
        }
    }
}