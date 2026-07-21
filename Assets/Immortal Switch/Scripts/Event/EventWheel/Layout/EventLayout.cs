using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Event.EventWheel.Controller;
using Immortal_Switch.Scripts.Event.EventWheel.UI;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.UI;
using Nakama;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Immortal_Switch.Scripts.Event.EventWheel.Layout
{
    public enum EEventCategory
    {
        Normal = 1,
        Premium = 2,
    }

    [Serializable]
    public class EventLayoutCategory
    {
        public EEventCategory type;
        public WheelController controller;
        public UIEventWheelCategory category;
    }

    public class EventLayout : MonoBehaviour
    {
        [Header("Header references")]
        [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField]
        private TextMeshProUGUI txtCountdown;

        [SerializeField]
        private Toggle toggleSkipAnimation;

        [Header("References")]
        [SerializeField]
        private Button btnNormal;

        [SerializeField]
        private Button btnPremium;

        [SerializeField]
        private UIEventWheelButtonSpin btnX1Normal;

        [SerializeField]
        private UIEventWheelButtonSpin btnX10Normal;

        [SerializeField]
        private UIEventWheelButtonSpin btnX1Premium;

        [SerializeField]
        private UIEventWheelButtonSpin btnX10Premium;

        [Header("Wheel references")]
        [SerializeField]
        private List<EventLayoutCategory> categories = new();

        // --- Private Fields ---
        private EventLayoutCategory _selectedCategory;

        private int _normalX1;
        private int _normalX10;

        private int _premiumX1;
        private int _premiumX10;
        private bool _isRolling;
        private CancellationTokenSource _spinCancellationTokenSource;
        private UniTaskCompletionSource _spinCompletionSource;

        private void Awake()
        {
            btnNormal.onClick.AddListener(OnClickNormal);
            btnPremium.onClick.AddListener(OnClickPremium);
        }

        private void OnDestroy()
        {
            CancelCurrentSpin();
            btnNormal.onClick.RemoveListener(OnClickNormal);
            btnPremium.onClick.RemoveListener(OnClickPremium);
        }

        private void OnEnable()
        {
            UnselectCategories();
            OnClickNormal();
        }

        private void OnDisable()
        {
            CancelCurrentSpin();
        }

        private void OnClickNormal()
        {
            txtTitle.text = "Vòng quay cơ bản";
            SetSelected(EEventCategory.Normal);
        }

        private void OnClickPremium()
        {
            txtTitle.text = "Vòng quay cao cấp";
            SetSelected(EEventCategory.Premium);
        }

        public void Bind(
            int normalX1, int normalX10, int premiumX1, int premiumX10,
            List<DynamicHeroesGlobalSpecificationsEventWheelRewardsPoolRow> normalItems,
            List<DynamicHeroesGlobalSpecificationsEventWheelRewardsPoolRow> premiumItems
        )
        {
            _normalX1 = normalX1;
            _normalX10 = normalX10;

            _premiumX1 = premiumX1;
            _premiumX10 = premiumX10;

            btnX1Normal.Bind(1, $"{_normalX1}", OnClickSpinNormal);
            btnX10Normal.Bind(10, $"{_normalX10}", OnClickSpinNormal);

            btnX1Premium.Bind(1, $"{_premiumX1}", OnClickSpinPremium);
            btnX10Premium.Bind(10, $"{_premiumX10}", OnClickSpinPremium);

            foreach (var category in categories)
            {
                switch (category.type)
                {
                    case EEventCategory.Normal:
                        category.controller.Bind(normalItems);
                        break;

                    case EEventCategory.Premium:
                        category.controller.Bind(premiumItems);
                        break;
                }
            }
        }

        private void OnClickSpinNormal(int times)
        {
            StartSpin(EEventCategory.Normal, times).Forget();
        }

        private void OnClickSpinPremium(int times)
        {
            StartSpin(EEventCategory.Premium, times).Forget();
        }

        private async UniTask StartSpin(EEventCategory type, int times)
        {
            if (_isRolling ||
                times <= 0 ||
                _selectedCategory == null ||
                _selectedCategory.type != type)
            {
                return;
            }

            _isRolling = true;

            var controller = _selectedCategory.controller;

            // Kết quả quay phải lấy từ server TRƯỚC khi chạy animation — server trừ vé và random
            // có trọng số (weight) 1 lần duy nhất cho cả batch, client chỉ diễn hoạt lại đúng
            // slot_index server trả về, không tự random nữa.
            EventWheelSpinResponse response;

            try
            {
                response = await NakamaClient.Instance.EventWheelSpinAsync(new EventWheelSpinRequest
                {
                    Category = (int)type,
                    Times    = times,
                });
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventLayout] eventwheel/spin error {ex.StatusCode}: {ex.Message}");
                _isRolling = false;
                return;
            }

            if (!response.Success ||
                response.Entries == null ||
                response.Entries.Count == 0)
            {
                Debug.LogWarning($"[EventLayout] eventwheel/spin failed: {response.Error}");
                UIManager.Instance.ShowToast(DescribeSpinError(response.Error));
                _isRolling = false;
                return;
            }

            var spinCancellationTokenSource = new CancellationTokenSource();

            _spinCancellationTokenSource = spinCancellationTokenSource;

            var cancellationToken = spinCancellationTokenSource.Token;
            var rewards = new List<ItemData>();
            var force = toggleSkipAnimation.isOn;
            var entries = response.Entries;

            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];

                    controller.StartSpin();

                    if (!force)
                    {
                        var isCanceled = await UniTask
                            .Delay(1000, cancellationToken: cancellationToken)
                            .SuppressCancellationThrow();

                        if (isCanceled)
                        {
                            return;
                        }
                    }

                    var completionSource = new UniTaskCompletionSource();
                    _spinCompletionSource = completionSource;

                    // slot_index (server) đánh số từ 1, WheelController.StopAt cần index 0-based.
                    controller.StopAt(
                        entry.SlotIndex - 1,
                        force,
                        () => completionSource.TrySetResult()
                    );

                    var isCompletionCanceled = await completionSource.Task.SuppressCancellationThrow();

                    if (_spinCompletionSource == completionSource)
                    {
                        _spinCompletionSource = null;
                    }

                    if (isCompletionCanceled)
                    {
                        return;
                    }

                    AddReward(entry);

                    if (i < entries.Count - 1)
                    {
                        var stepDelay = Random.Range(1000, 2001);

                        var isCanceled = await UniTask
                            .Delay(stepDelay, cancellationToken: cancellationToken)
                            .SuppressCancellationThrow();

                        if (isCanceled)
                        {
                            return;
                        }
                    }
                }

                CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
                await EventWheelPassManager.Instance.RefreshAsync();

                if (rewards.Count > 0)
                {
                    await UIManager.Instance
                        .OpenPopupAsync<PopupRewardView>(new PopupRewardArgs
                        {
                            Rewards = rewards,
                        });
                }
            }
            finally
            {
                if (_spinCancellationTokenSource == spinCancellationTokenSource)
                {
                    _spinCancellationTokenSource.Dispose();
                    _spinCancellationTokenSource = null;
                    _spinCompletionSource = null;
                    _isRolling = false;
                }
            }

            void AddReward(EventWheelSpinEntryDto entry)
            {
                if (entry?.Item != null)
                {
                    rewards.Add(new ItemData(entry.Item.ItemId, entry.Item.Amount));
                }
            }
        }

        private static string DescribeSpinError(string error)
        {
            switch (error)
            {
                case "EVENT_NOT_ACTIVE":    return "Sự kiện không còn hoạt động.";
                case "INSUFFICIENT_TICKET": return "Không đủ vé để quay.";
                case "POOL_EMPTY":          return "Vòng quay chưa có phần thưởng, thử lại sau.";
                default:                    return "Quay thất bại, vui lòng thử lại.";
            }
        }

        private void SetSelected(EEventCategory type)
        {
            if (_isRolling &&
                _selectedCategory != null &&
                _selectedCategory.type != type)
            {
                CancelCurrentSpin();
            }

            if (_selectedCategory != null)
            {
                _selectedCategory.controller.gameObject.SetActive(false);
                _selectedCategory.category.SetSelected(false);
                _selectedCategory = null;
            }

            foreach (var category in categories)
            {
                if (category.type == type)
                {
                    _selectedCategory = category;
                    _selectedCategory.controller.gameObject.SetActive(true);
                    _selectedCategory.category.SetSelected(true);
                    break;
                }
            }
        }

        private void UnselectCategories()
        {
            foreach (var category in categories)
            {
                category.category.SetSelected(false);
                category.controller.gameObject.SetActive(false);
            }
        }

        /// <summary>Hủy chuỗi quay và completion source hiện tại khi đổi tab.</summary>
        private void CancelCurrentSpin()
        {
            _spinCancellationTokenSource?.Cancel();
            _spinCancellationTokenSource?.Dispose();
            _spinCancellationTokenSource = null;

            _spinCompletionSource?.TrySetCanceled();
            _spinCompletionSource = null;
            _isRolling = false;
        }
    }
}