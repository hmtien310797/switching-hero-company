using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Event.EventWheel.Interfaces;
using Immortal_Switch.Scripts.Items.Models;

namespace Immortal_Switch.Scripts.Event.EventWheel
{
    public class EventWheelPassManager : Singleton<EventWheelPassManager>
    {
        public IEventWheelPassService Service { get; private set; }
        public IEventWheelPassStorage Storage { get; private set; }

        public event Action OnDataChanged;

        protected override void OnSingletonAwake()
        {
            Load();
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        public int GetPurchasedSpinCount(int eventId)
        {
            return Service.GetPurchasedSpinCount(eventId);
        }

        public void RecordSpinPurchase(int eventId, int amount)
        {
            if (Service.RecordSpinPurchase(eventId, amount))
            {
                OnDataChanged?.Invoke();
            }
        }

        /// <summary>Lấy số lần đã mua của một ô shop theo loại giới hạn trong cấu hình.</summary>
        public int GetShopPurchasedCount(DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow row)
        {
            return Service.GetShopPurchasedCount(row);
        }

        /// <summary>Lấy số lượt mua còn lại của một ô shop.</summary>
        public int GetShopRemaining(DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow row)
        {
            return Service.GetShopRemaining(row);
        }

        /// <summary>Kiểm tra ô shop có thể mua thêm số lượt yêu cầu hay không.</summary>
        public bool CanPurchaseShopItem(
            DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow row,
            int purchaseCount = 1
        )
        {
            return Service.CanPurchaseShopItem(row, purchaseCount);
        }

        /// <summary>Ghi nhận lượt mua shop nếu chưa vượt giới hạn.</summary>
        public bool RecordShopPurchase(
            DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow row,
            int purchaseCount = 1
        )
        {
            if (!Service.RecordShopPurchase(row, purchaseCount))
            {
                return false;
            }

            OnDataChanged?.Invoke();
            return true;
        }

        public bool IsPremiumPurchased(int eventId)
        {
            return Service.IsPremiumPurchased(eventId);
        }

        public void PurchasePremium(int eventId)
        {
            if (Service.PurchasePremium(eventId))
            {
                OnDataChanged?.Invoke();
            }
        }

        public bool IsFreeClaimed(int eventId, int milestoneId)
        {
            return Service.IsFreeClaimed(eventId, milestoneId);
        }

        public bool IsPremiumClaimed(int eventId, int milestoneId)
        {
            return Service.IsPremiumClaimed(eventId, milestoneId);
        }

        public bool CanClaim(
            int eventId,
            DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow row
        )
        {
            return Service.CanClaim(eventId, row);
        }

        public bool HasClaimable(
            int eventId,
            IReadOnlyList<DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow> rows
        )
        {
            return Service.HasClaimable(eventId, rows);
        }

        public List<ItemRewardData> ClaimMilestone(
            int eventId,
            DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow row
        )
        {
            var result = Service.ClaimMilestone(eventId, row);

            if (result.Changed)
            {
                OnDataChanged?.Invoke();
            }

            return result.Rewards;
        }

        public List<ItemRewardData> ClaimAll(
            int eventId,
            IReadOnlyList<DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow> rows
        )
        {
            var result = Service.ClaimAll(eventId, rows);

            if (result.Changed)
            {
                OnDataChanged?.Invoke();
            }

            return result.Rewards;
        }

        public void ResetEvent(int eventId)
        {
            if (Service.ResetEvent(eventId))
            {
                OnDataChanged?.Invoke();
            }
        }

        private void Load()
        {
            Storage = new EventWheelPassStorage();
            Storage.Load();
            Service = new EventWheelPassService(Storage);
        }
    }
}
