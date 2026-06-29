using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.UI
{
    public sealed class ToastMessageView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text messageText;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float fadeInDuration = 0.18f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.22f;
        [SerializeField, Min(0f)] private float defaultDisplayDuration = 1.5f;

        [SerializeField]
        private float startYOffset = -25f;

        [SerializeField]
        private float startScale = 0.96f;

        private Sequence _toastSequence;
        private Vector2 _visibleAnchoredPosition;

        private void Awake()
        {
            if (contentRoot == null)
                contentRoot = transform as RectTransform;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            _visibleAnchoredPosition = contentRoot.anchoredPosition;

            HideImmediate();
        }

        /// <summary>
        /// Hiển thị toast.
        /// Nếu toast đang chạy thì animation cũ sẽ bị huỷ và hiển thị message mới.
        /// </summary>
        public void Show(string message, float displayDuration = -1f)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                HideImmediate();
                return;
            }

            if (displayDuration < 0f)
                displayDuration = defaultDisplayDuration;

            KillCurrentAnimation();

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            messageText.text = message;

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            contentRoot.anchoredPosition =
                _visibleAnchoredPosition + new Vector2(0f, startYOffset);

            contentRoot.localScale = Vector3.one * startScale;

            _toastSequence = DOTween.Sequence()
                .SetUpdate(true);

            _toastSequence
                .Append(canvasGroup
                    .DOFade(1f, fadeInDuration)
                    .SetEase(Ease.OutCubic));

            _toastSequence.Join(contentRoot
                .DOAnchorPos(_visibleAnchoredPosition, fadeInDuration)
                .SetEase(Ease.OutCubic));

            _toastSequence.Join(contentRoot
                .DOScale(Vector3.one, fadeInDuration)
                .SetEase(Ease.OutBack));

            _toastSequence.AppendInterval(displayDuration);

            _toastSequence.Append(canvasGroup
                .DOFade(0f, fadeOutDuration)
                .SetEase(Ease.InCubic));

            _toastSequence.Join(contentRoot
                .DOAnchorPos(
                    _visibleAnchoredPosition + new Vector2(0f, 10f),
                    fadeOutDuration)
                .SetEase(Ease.InCubic));

            _toastSequence.OnComplete(HideImmediate);
        }

        public void HideImmediate()
        {
            KillCurrentAnimation();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (contentRoot != null)
            {
                contentRoot.anchoredPosition = _visibleAnchoredPosition;
                contentRoot.localScale = Vector3.one;
            }

            gameObject.SetActive(false);
        }

        private void KillCurrentAnimation()
        {
            if (_toastSequence != null)
            {
                _toastSequence.Kill(false);
                _toastSequence = null;
            }

            if (canvasGroup != null)
                canvasGroup.DOKill(false);

            if (contentRoot != null)
                contentRoot.DOKill(false);
        }

        private void OnDestroy()
        {
            KillCurrentAnimation();
        }
    }
}