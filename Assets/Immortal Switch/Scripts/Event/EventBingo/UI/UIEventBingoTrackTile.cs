using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Shared.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventBingo.UI
{
    public class UIEventBingoTrackTile : MonoBehaviour
    {
        [SerializeField]
        private UIRewardQuantity rewardQuantity;

        [SerializeField]
        private GameObject goClaimed;

        [SerializeField]
        private GameObject goLocked;

        [Header("Reveal animation")]
        [SerializeField]
        private Ease revealInEase = Ease.InCubic;

        [SerializeField]
        private Ease revealOutEase = Ease.OutCubic;

        private RectTransform _rectTransform;
        private Tweener _revealTween;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
        }

        private void OnDestroy()
        {
            _revealTween?.Kill();
        }

        public void Bind(int itemId, BigNumber quantity, bool isLocked)
        {
            rewardQuantity.Bind(itemId, quantity);
            SetLocked(isLocked);
        }

        public void SetClaimed(bool value)
        {
            goClaimed.SetActive(value);

            if (value)
            {
                SetLocked(false);
            }
        }

        /// <summary>Bật hoặc tắt lớp khóa đang che tile.</summary>
        public void SetLocked(bool value)
        {
            goLocked.SetActive(value);
        }

        /// <summary>
        /// Lật tile tại chỗ. Tile giữ lock ở nửa đầu, sau đó hiện reward nhưng chưa claimed.
        /// </summary>
        public async UniTask PlayRevealAsync(
            float duration,
            CancellationToken cancellationToken
        )
        {
            _revealTween?.Kill();

            SetLocked(true);
            SetClaimed(false);

            _rectTransform.localEulerAngles = Vector3.zero;

            var halfDuration = Mathf.Max(0.01f, duration * 0.5f);

            try
            {
                _revealTween = _rectTransform
                    .DOLocalRotate(new Vector3(0f, 90f, 0f), halfDuration)
                    .SetEase(revealInEase)
                    .SetLink(gameObject);

                await _revealTween.ToUniTask(
                    TweenCancelBehaviour.Kill,
                    cancellationToken
                );

                SetLocked(false);
                SetClaimed(false);

                _rectTransform.localEulerAngles = new Vector3(0f, -90f, 0f);

                _revealTween = _rectTransform
                    .DOLocalRotate(Vector3.zero, halfDuration)
                    .SetEase(revealOutEase)
                    .SetLink(gameObject);

                await _revealTween.ToUniTask(
                    TweenCancelBehaviour.Kill,
                    cancellationToken
                );
            }
            finally
            {
                _revealTween = null;
                _rectTransform.localEulerAngles = Vector3.zero;
            }
        }
    }
}