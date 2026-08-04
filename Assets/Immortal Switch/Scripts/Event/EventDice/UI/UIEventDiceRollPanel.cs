using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventDice.UI
{
    public class UIEventDiceRollPanel : MonoBehaviour
    {
        [Header("Dice references")]
        [PreviewField]
        [SerializeField]
        private List<Sprite> dices = new();

        [SerializeField]
        private Image imgDice;

        [SerializeField]
        private Button btnRoll;

        [SerializeField]
        private Button btnClaimPendingRewards;

        [Header("Reward preview references")]
        [SerializeField]
        private RectTransform rewardPreviewContainer;

        [SerializeField]
        private UIRewardQuantity rewardPreviewPrefab;

        [Header("Dice animation references")]
        [SerializeField]
        [Range(0f, float.MaxValue)]
        private float rotateDuration = 1f;

        [SerializeField]
        [Min(0.01f)]
        private float spriteChangeInterval = 0.08f;

        // --- Private Fields ---
        private SimpleUIPool<UIRewardQuantity> _pool;
        private Tweener _tweenerDiceRotate;
        private CancellationTokenSource _rollCancellation;
        private CancellationTokenSource _spriteCancellation;

        private Func<CancellationToken, UniTask<int>> _onStartRoll;
        private Func<int, UniTask> _onRollCompleted;
        private Action _onClaimPendingRewards;

        private bool _isRolling;

        private void Awake()
        {
            btnRoll.onClick.AddListener(OnClickRollDice);
            btnClaimPendingRewards.onClick.AddListener(OnClickClaimPendingRewards);
        }

        private void OnClickClaimPendingRewards()
        {
            _onClaimPendingRewards?.Invoke();
        }

        private void OnClickRollDice()
        {
            RollAsync().Forget();
        }

        private void OnDestroy()
        {
            btnClaimPendingRewards.onClick.RemoveListener(OnClickClaimPendingRewards);
            btnRoll.onClick.RemoveListener(OnClickRollDice);
            _rollCancellation?.Cancel();
            KillAnimation();
        }

        public void Bind(
            List<ItemData> pendingRewards,
            Func<CancellationToken, UniTask<int>> onStartRoll,
            Func<int, UniTask> onRollCompleted,
            Action onClaimPendingRewards
        )
        {
            _onStartRoll = onStartRoll;
            _onRollCompleted = onRollCompleted;
            _onClaimPendingRewards = onClaimPendingRewards;

            RefreshPendingRewards(pendingRewards);
        }

        /// <summary>
        /// Bắt đầu xoay và thay đổi liên tục các sprite xúc xắc trong lúc chờ server.
        /// </summary>
        public void StartRoll(CancellationToken cancellationToken)
        {
            KillAnimation();

            _spriteCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken
            );

            AnimateDiceSpritesAsync(_spriteCancellation.Token).Forget();

            _tweenerDiceRotate = imgDice.transform
                .DOLocalRotate(Vector3.back * 360f, rotateDuration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental)
                .SetLink(gameObject);
        }

        /// <summary>
        /// Dừng animation và hiển thị chính xác kết quả xúc xắc từ 1 đến số sprite hiện có.
        /// </summary>
        public void StopRoll(int result)
        {
            if (result <= 0 ||
                result > dices.Count)
            {
                Debug.LogError(
                    $"[UIEventDiceRollPanel] Result phải nằm trong khoảng 1..{dices.Count}, " +
                    $"nhưng nhận được {result}."
                );

                KillAnimation();
                return;
            }

            KillAnimation();

            imgDice.transform.localRotation = Quaternion.identity;
            imgDice.sprite = dices[result - 1];
        }

        private async UniTask RollAsync()
        {
            if (_isRolling ||
                _onStartRoll == null ||
                dices.Count == 0)
            {
                return;
            }

            _isRolling = true;
            btnRoll.interactable = false;

            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy()
            );

            _rollCancellation = cancellation;

            StartRoll(cancellation.Token);

            try
            {
                var result = await _onStartRoll(cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                StopRoll(result);

                if (_onRollCompleted != null)
                {
                    await _onRollCompleted(result);
                }
            }
            catch (OperationCanceledException)
            {
                KillAnimation();
            }
            catch (Exception exception)
            {
                KillAnimation();
                Debug.LogException(exception);
            }
            finally
            {
                if (ReferenceEquals(_rollCancellation, cancellation))
                {
                    _rollCancellation = null;
                }

                cancellation.Dispose();

                _isRolling = false;
                btnRoll.interactable = true;
            }
        }

        private async UniTask AnimateDiceSpritesAsync(CancellationToken cancellationToken)
        {
            var spriteIndex = 0;

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    imgDice.sprite = dices[spriteIndex];
                    spriteIndex = (spriteIndex + 1) % dices.Count;

                    await UniTask.Delay(
                        TimeSpan.FromSeconds(spriteChangeInterval),
                        cancellationToken: cancellationToken
                    );
                }
            }
            catch (OperationCanceledException)
            {
                // StopRoll hoặc vòng đời GameObject đã dừng animation đổi sprite.
            }
        }

        private void KillAnimation()
        {
            _spriteCancellation?.Cancel();
            _spriteCancellation?.Dispose();
            _spriteCancellation = null;

            _tweenerDiceRotate?.Kill();
            _tweenerDiceRotate = null;
        }

        /// <summary>
        /// Refresh danh sách phần thưởng đang chờ nhận.
        /// </summary>
        public void RefreshPendingRewards(List<ItemData> pendingRewards)
        {
            _pool ??= new SimpleUIPool<UIRewardQuantity>(rewardPreviewPrefab, rewardPreviewContainer);

            for (var index = 0; index < pendingRewards.Count; index++)
            {
                var reward = pendingRewards[index];
                var clone = _pool.Get(index);
                clone.Bind(reward.ItemId, reward.Quantity);
            }

            _pool.ReleaseFrom(pendingRewards.Count);
        }
    }
}