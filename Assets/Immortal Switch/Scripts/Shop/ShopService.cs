using System;
using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Helper;
using Immortal_Switch.Scripts.Shop.Interfaces;
using Immortal_Switch.Scripts.Shop.Models;
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

            // Reset monthly pass đã hết hạn (quá 30 ngày)
            ResetExpiredMonthlyPasses();
        }

        private void ResetExpiredMonthlyPasses()
        {
            var expired = _storage.Data.MonthlyPasses.Keys
                .Where(packId => GetMonthlyPassCurrentDay(packId) > 30)
                .ToList();

            if (expired.Count == 0)
            {
                return;
            }

            foreach (var packId in expired)
            {
                _storage.Data.MonthlyPasses.Remove(packId);
            }

            _storage.Save();
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

        // ── Topup / GloryPass ────────────────────────────────────────────────

        public int GetTopupCount()
        {
            return _storage.Data.TotalTopupCount;
        }

        public void RecordTopup()
        {
            _storage.Data.TotalTopupCount++;
            _storage.Save();
        }

        public bool IsGloryPassClaimed(int milestoneId)
        {
            return _storage.Data.ClaimedGloryPassIds.Contains(milestoneId);
        }

        public void ClaimGloryPass(int milestoneId)
        {
            if (!IsGloryPassClaimed(milestoneId))
            {
                _storage.Data.ClaimedGloryPassIds.Add(milestoneId);
                _storage.Save();
            }
        }

        public void ResetGloryPassClaims()
        {
            if (_storage.Data.ClaimedGloryPassIds.Count > 0)
            {
                _storage.Data.ClaimedGloryPassIds.Clear();
                _storage.Save();
            }
        }

        // ── Monthly Pass ──────────────────────────────────────────────────────

        private MonthlyPassData GetOrCreatePassData(int packId)
        {
            if (!_storage.Data.MonthlyPasses.TryGetValue(packId, out var data))
            {
                data = new MonthlyPassData();
                _storage.Data.MonthlyPasses[packId] = data;
            }

            return data;
        }

        /// <summary>Đã mua monthly pass packId chưa (còn trong hạn 30 ngày).</summary>
        public bool IsMonthlyPassPurchased(int packId)
        {
            return _storage.Data.MonthlyPasses.ContainsKey(packId) && GetMonthlyPassCurrentDay(packId) <= 30;
        }

        /// <summary>Ngày mua monthly pass.</summary>
        public DateTime? GetMonthlyPassPurchaseDate(int packId)
        {
            return _storage.Data.MonthlyPasses.TryGetValue(packId, out var data)
                ? data.PurchaseDate
                : null;
        }

        /// <summary>Ngày hiện tại tính từ lúc mua (1-based). Trả về 1 nếu chưa mua, >30 nếu đã hết hạn.</summary>
        public int GetMonthlyPassCurrentDay(int packId)
        {
            if (!_storage.Data.MonthlyPasses.TryGetValue(packId, out var data))
            {
                return 1;
            }

            return (DateTime.UtcNow.Date - data.PurchaseDate.Date).Days + 1;
        }

        /// <summary>Kiểm tra ngày thứ day đã nhận thưởng chưa.</summary>
        public bool IsMonthlyPassDayClaimed(int packId, int day)
        {
            return _storage.Data.MonthlyPasses.TryGetValue(packId, out var data) && data.ClaimedDays.Contains(day);
        }

        /// <summary>Đánh dấu đã nhận thưởng cho ngày thứ day. Hết 30 ngày, pass tự động hết hạn vào ngày hôm sau.</summary>
        public void ClaimMonthlyPassDay(int packId, int day)
        {
            var data = GetOrCreatePassData(packId);

            if (!data.ClaimedDays.Contains(day))
            {
                data.ClaimedDays.Add(day);
                _storage.Save();
            }
        }

        /// <summary>Ghi nhận giao dịch mua monthly pass.</summary>
        public void PurchaseMonthlyPass(int packId)
        {
            var data = new MonthlyPassData
            {
                PurchaseDate = DateTime.UtcNow,
                ClaimedDays = new List<int>(),
            };

            _storage.Data.MonthlyPasses[packId] = data;
            _storage.Save();
        }
    }
}