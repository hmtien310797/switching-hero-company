using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.UI;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Popup
{
    public class PopupEventLeHoiBangLongSummonArgs
    {
        /// <summary>
        /// co skip animation hay ko
        /// </summary>
        public readonly bool IsSkipAnimation;

        /// <summary>
        /// on click summon
        /// 1: số lần roll
        /// 2: return rewards
        /// </summary>
        public readonly Func<int, UniTask<List<ItemData>>> TrySummon;

        /// <summary>
        /// ds phan thuong lan dau xuat hien
        /// </summary>
        public readonly List<ItemData> FirstRewards;

        public PopupEventLeHoiBangLongSummonArgs(
            bool isSkipAnimation,
            Func<int, UniTask<List<ItemData>>> trySummon,
            List<ItemData> firstRewards
        )
        {
            TrySummon = trySummon;
            FirstRewards = firstRewards;
            IsSkipAnimation = isSkipAnimation;
        }
    }

    public class PopupEventLeHoiBangLongSummonView : AnimatedUIView
    {
        [Header("Summon references")]
        [SerializeField]
        private Toggle tgSkipAnimation;

        [SerializeField]
        private Toggle tgAutoAnimation;

        [SerializeField]
        private UIEventLeHoiBangLongSummonButton btnX1;

        [SerializeField]
        private UIEventLeHoiBangLongSummonButton btnX10;

        [SerializeField]
        private Button btnClose;

        [Header("Card references")]
        [SerializeField]
        private RectTransform cardContainer;

        [SerializeField]
        private UIRewardQuantity cardPrefab;

        [Header("Animation references")]
        [SerializeField]
        [Range(0f, 1f)]
        private float itemEffectInterval;

        [SerializeField]
        [Range(0f, 1f)]
        private float itemScaleDuration;

        // --- Private Fields ---
        private List<Tweener> _cardTweeners = new();
        private SimpleUIPool<UIRewardQuantity> _cardPools;
        private PopupEventLeHoiBangLongSummonArgs _args;

        private bool _isAutoAnimation;
        private bool _isSkipAnimation;
        private bool _isSummoning;
        private CancellationTokenSource _summonCancellation;

        private void Awake()
        {
            tgAutoAnimation.onValueChanged.AddListener(OnToggleAutoAnimationChanged);
            tgSkipAnimation.onValueChanged.AddListener(OnToggleSkipAnimationChanged);
            btnClose.onClick.AddListener(OnClickClose);
        }

        private void OnClickClose()
        {
            CancelSummon();
            UIManager.Instance.Close<PopupEventLeHoiBangLongSummonView>();
        }

        private void OnDestroy()
        {
            CancelSummon();
            tgAutoAnimation.onValueChanged.RemoveListener(OnToggleAutoAnimationChanged);
            tgSkipAnimation.onValueChanged.RemoveListener(OnToggleSkipAnimationChanged);
            btnClose.onClick.RemoveListener(OnClickClose);
        }

        private void OnToggleSkipAnimationChanged(bool arg0)
        {
            _isSkipAnimation = arg0;
        }

        private void OnToggleAutoAnimationChanged(bool arg0)
        {
            _isAutoAnimation = arg0;
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);
            CancelSummon();

            if (args is not PopupEventLeHoiBangLongSummonArgs runtime)
            {
                return;
            }

            _args = runtime;
            _isSkipAnimation = _args.IsSkipAnimation;
            _isAutoAnimation = tgAutoAnimation.isOn;

            tgSkipAnimation.SetIsOnWithoutNotify(_args.IsSkipAnimation);

            btnX1.Bind(1, OnClickSummon);
            btnX10.Bind(10, OnClickSummon);
            ShowRewardImmediate();
        }

        public override void OnHide()
        {
            CancelSummon();
            base.OnHide();
        }

        private void ShowRewardImmediate()
        {
            var cancellation = CancellationTokenOnDestroy();
            RefreshRewardsAsync(_args.FirstRewards, cancellation.Token).Forget();
        }

        private CancellationTokenSource CancellationTokenOnDestroy()
        {
            return CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy()
            );
        }

        private void OnClickSummon(int times)
        {
            if (_args?.TrySummon == null ||
                _isSummoning)
            {
                return;
            }

            _isSummoning = true;

            var cancellation = CancellationTokenOnDestroy();

            _summonCancellation = cancellation;

            SummonLoopAsync(times, _args.TrySummon, cancellation).Forget();
        }

        private void Kill()
        {
            foreach (var t in _cardTweeners)
            {
                t.Kill();
            }

            _cardTweeners.Clear();
        }

        private async UniTaskVoid SummonLoopAsync(
            int times,
            Func<int, UniTask<List<ItemData>>> trySummon,
            CancellationTokenSource cancellation)
        {
            var cancellationToken = cancellation.Token;

            try
            {
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var rewards = await trySummon(times);

                    cancellationToken.ThrowIfCancellationRequested();

                    if (rewards == null ||
                        rewards.Count < 1)
                    {
                        Kill();
                        break;
                    }

                    Kill();

                    await RefreshRewardsAsync(rewards, cancellationToken);

                    if (rewards.Count == 0)
                    {
                        break;
                    }
                } while (_isAutoAnimation && isActiveAndEnabled);
            }
            catch (OperationCanceledException)
            {
                // Popup đã đóng hoặc một lượt summon mới đã thay thế lượt hiện tại.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                cancellation.Dispose();

                if (ReferenceEquals(_summonCancellation, cancellation))
                {
                    _summonCancellation = null;
                    _isSummoning = false;
                }
            }
        }

        private async UniTask RefreshRewardsAsync(
            IReadOnlyList<ItemData> rewards,
            CancellationToken cancellationToken)
        {
            _cardPools ??= new SimpleUIPool<UIRewardQuantity>(cardPrefab, cardContainer);
            Tweener lastTweener = null;

            for (int i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];
                var clone = _cardPools.Get(i);

                clone.Bind(reward.ItemId, reward.Quantity);

                if (!_isSkipAnimation)
                {
                    clone.transform.localScale = Vector3.zero;

                    var tweener = clone.transform
                        .DOScale(Vector3.one, itemScaleDuration)
                        .SetDelay((i + 1) * itemEffectInterval)
                        .SetLink(clone.gameObject);

                    _cardTweeners.Add(tweener);

                    lastTweener = tweener;
                }
                else
                {
                    clone.transform.localScale = Vector3.one;
                }
            }

            _cardPools.ReleaseFrom(rewards.Count);

            if (lastTweener != null)
            {
                await lastTweener.ToUniTask(
                    TweenCancelBehaviour.Kill,
                    cancellationToken
                );

                return;
            }

            if (_isAutoAnimation && _isSkipAnimation)
            {
                // Skip animation vẫn nhường một frame để auto không tạo vòng lặp đồng bộ.
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
            }
        }

        private void CancelSummon()
        {
            var cancellation = _summonCancellation;
            _summonCancellation = null;
            _isSummoning = false;

            if (cancellation != null &&
                !cancellation.IsCancellationRequested)
            {
                cancellation.Cancel();
            }

            Kill();
        }
    }
}