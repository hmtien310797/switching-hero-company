using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Helper;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Helper;
using Immortal_Switch.Scripts.Shop.Interfaces;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shop
{
    internal class ShopService : IShopService
    {
        private readonly IShopStorage _storage;

        public ShopService(IShopStorage storage)
        {
            _storage = storage;
        }

        public int GetRemaining(int packId, int limitFromConfig)
        {
            if (limitFromConfig <= 0)
            {
                return int.MaxValue;
            }

            var purchased = GetPurchasedCount(packId);
            return Mathf.Max(0, limitFromConfig - purchased);
        }

        public int GetPurchasedCount(int packId)
        {
            return _storage.Data.PurchaseCount.GetValueOrDefault(packId);
        }

        public void RecordPurchase(int packId)
        {
            var count = GetPurchasedCount(packId);
            _storage.Data.PurchaseCount[packId] = count + 1;
            _storage.Save();
        }

        public void CheckAndReset()
        {
            var now = DateTime.UtcNow;
            var changed = false;

            if (_storage.Data.DailyResetDate == null ||
                DateTimeHelper.IsNewDay(_storage.Data.DailyResetDate.Value))
            {
                _storage.Data.DailyResetDate = now;
                ResetPurchasesByCycle("daily");
                changed = true;
            }

            if (_storage.Data.WeeklyResetDate == null ||
                IsNewWeek(_storage.Data.WeeklyResetDate.Value))
            {
                _storage.Data.WeeklyResetDate = now;
                ResetPurchasesByCycle("weekly");
                changed = true;
            }

            if (_storage.Data.MonthlyResetDate == null ||
                IsNewMonth(_storage.Data.MonthlyResetDate.Value))
            {
                _storage.Data.MonthlyResetDate = now;
                ResetPurchasesByCycle("monthly");
                changed = true;
            }

            if (changed)
            {
                _storage.Save();
            }
        }

        private void ResetPurchasesByCycle(string cycle)
        {
            var packs = DatabaseManager.Instance.GetShopPacksSpecial();

            if (packs == null)
            {
                return;
            }

            foreach (var pack in packs)
            {
                if (pack.Pack.limitReset == cycle)
                {
                    _storage.Data.PurchaseCount.Remove(pack.Pack.iD);
                }
            }
        }

        private static bool IsNewWeek(DateTime date)
        {
            var now = DateTime.UtcNow;
            var daysSinceMonday = ((int)now.DayOfWeek - 1 + 7) % 7;
            var mondayThisWeek = now.Date.AddDays(-daysSinceMonday);
            return date.Date < mondayThisWeek;
        }

        private static bool IsNewMonth(DateTime date)
        {
            var now = DateTime.UtcNow;
            var firstOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return date.Date < firstOfMonth;
        }
    }
}