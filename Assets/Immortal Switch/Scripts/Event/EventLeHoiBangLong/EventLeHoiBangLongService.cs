using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Interfaces;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Models;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong
{
    internal class EventLeHoiBangLongService : IEventLeHoiBangLongService
    {
        private const int MAX_LOGIN_DAY = 7;

        private readonly IEventLeHoiBangLongStorage _storage;
        private readonly IReadOnlyList<DynamicHeroesGlobalSpecificationsEventBLDailyMissionRow> _missions;

        public EventLeHoiBangLongService(
            IEventLeHoiBangLongStorage storage,
            IReadOnlyList<DynamicHeroesGlobalSpecificationsEventBLDailyMissionRow> missions
        )
        {
            _storage = storage;
            _missions = missions ?? Array.Empty<DynamicHeroesGlobalSpecificationsEventBLDailyMissionRow>();
        }

        /// <summary>
        /// Khởi tạo dữ liệu, ghi nhận ngày đăng nhập và reset nhiệm vụ khi sang ngày mới.
        /// </summary>
        public bool Initialize(DateTime utcNow)
        {
            EnsureCollections();

            var changed = CheckDailyMissionResetInternal(utcNow);
            changed |= RegisterLoginInternal(utcNow);

            if (changed)
            {
                _storage.Save();
            }

            return changed;
        }

        /// <summary>
        /// Ghi nhận một ngày đăng nhập mới, tối đa bảy ngày.
        /// </summary>
        public bool RegisterLogin(DateTime utcNow)
        {
            var changed = RegisterLoginInternal(utcNow);

            if (changed)
            {
                _storage.Save();
            }

            return changed;
        }

        /// <summary>
        /// Kiểm tra và reset tiến độ nhiệm vụ hằng ngày, giữ nguyên tổng điểm và milestone.
        /// </summary>
        public bool CheckDailyMissionReset(DateTime utcNow)
        {
            var changed = CheckDailyMissionResetInternal(utcNow);

            if (changed)
            {
                _storage.Save();
            }

            return changed;
        }

        /// <summary>
        /// Kiểm tra phần thưởng miễn phí của ngày có thể nhận hay không.
        /// </summary>
        public bool CanClaimFreeLoginReward(int day)
        {
            return IsValidDay(day) &&
                   day <= _storage.Data.loginDay &&
                   !IsFreeLoginRewardClaimed(day);
        }

        /// <summary>
        /// Đánh dấu phần thưởng miễn phí của ngày đã được nhận.
        /// </summary>
        public bool ClaimFreeLoginReward(int day)
        {
            if (!CanClaimFreeLoginReward(day))
            {
                return false;
            }

            _storage.Data.claimedFreeLoginDays.Add(day);
            _storage.Save();
            return true;
        }

        /// <summary>
        /// Kiểm tra lượt thưởng miễn phí riêng trong panel bonus có thể nhận hay không.
        /// </summary>
        public bool CanClaimFreeBonus(int day)
        {
            return IsValidDay(day) &&
                   day <= _storage.Data.loginDay &&
                   !IsFreeBonusClaimed(day);
        }

        /// <summary>
        /// Đánh dấu lượt thưởng miễn phí riêng trong panel bonus đã được nhận.
        /// </summary>
        public bool ClaimFreeBonus(int day)
        {
            if (!CanClaimFreeBonus(day))
            {
                return false;
            }

            _storage.Data.claimedFreeBonusDays.Add(day);
            _storage.Save();
            return true;
        }

        /// <summary>
        /// Lấy trạng thái đã nhận lượt thưởng miễn phí riêng trong panel bonus.
        /// </summary>
        public bool IsFreeBonusClaimed(int day)
        {
            return _storage.Data.claimedFreeBonusDays.Contains(day);
        }

        /// <summary>
        /// Kiểm tra gói bonus trả phí của ngày đã đủ điều kiện để mua bằng IAP hay chưa.
        /// </summary>
        public bool CanPurchaseBonus(int day)
        {
            return IsValidDay(day) &&
                   IsFreeBonusClaimed(day) &&
                   !IsBonusPurchased(day);
        }

        /// <summary>
        /// Ghi nhận gói bonus đã mua sau khi thanh toán được xác nhận thành công.
        /// </summary>
        public bool ConfirmBonusPurchase(int day)
        {
            if (!CanPurchaseBonus(day))
            {
                return false;
            }

            _storage.Data.purchasedBonusDays.Add(day);
            _storage.Save();
            return true;
        }

        /// <summary>
        /// Kiểm tra phần thưởng bonus đã mua có thể nhận hay không.
        /// </summary>
        public bool CanClaimBonus(int day)
        {
            return IsValidDay(day) &&
                   IsBonusPurchased(day) &&
                   !IsBonusClaimed(day);
        }

        /// <summary>
        /// Đánh dấu phần thưởng bonus của ngày đã được nhận.
        /// </summary>
        public bool ClaimBonus(int day)
        {
            if (!CanClaimBonus(day))
            {
                return false;
            }

            _storage.Data.claimedBonusDays.Add(day);
            _storage.Save();
            return true;
        }

        /// <summary>
        /// Lấy trạng thái đã nhận phần thưởng miễn phí của ngày.
        /// </summary>
        public bool IsFreeLoginRewardClaimed(int day)
        {
            return _storage.Data.claimedFreeLoginDays.Contains(day);
        }

        /// <summary>
        /// Lấy trạng thái đã mua gói bonus của ngày.
        /// </summary>
        public bool IsBonusPurchased(int day)
        {
            return _storage.Data.purchasedBonusDays.Contains(day);
        }

        /// <summary>
        /// Lấy trạng thái đã nhận phần thưởng bonus của ngày.
        /// </summary>
        public bool IsBonusClaimed(int day)
        {
            return _storage.Data.claimedBonusDays.Contains(day);
        }

        /// <summary>
        /// Cộng tiến độ cho tất cả nhiệm vụ có trigger tương ứng.
        /// </summary>
        public bool ChangeMissionProgress(string trigger, int value)
        {
            if (string.IsNullOrWhiteSpace(trigger) ||
                value <= 0)
            {
                return false;
            }

            var changed = false;

            foreach (var row in _missions.Where(row => row.trigger == trigger))
            {
                var state = GetMissionState(row.missionId);

                if (state == null ||
                    state.isClaimed)
                {
                    continue;
                }

                state.progress = Math.Min(row.target, state.progress + value);
                changed = true;
            }

            if (changed)
            {
                _storage.Save();
            }

            return changed;
        }

        /// <summary>
        /// Lấy dữ liệu tiến độ của một nhiệm vụ theo ID.
        /// </summary>
        public EventLeHoiBangLongMissionState GetMissionState(string missionId)
        {
            return _storage.Data.missions.Find(state => state.missionId == missionId);
        }

        /// <summary>
        /// Nhận thưởng nhiệm vụ đã hoàn thành và cộng điểm nhiệm vụ.
        /// </summary>
        public bool ClaimMission(DynamicHeroesGlobalSpecificationsEventBLDailyMissionRow row)
        {
            if (row == null)
            {
                return false;
            }

            var state = GetMissionState(row.missionId);

            if (state == null ||
                state.isClaimed ||
                state.progress < row.target)
            {
                return false;
            }

            state.isClaimed = true;
            _storage.Data.missionPoints += Math.Max(0, row.points);

            _storage.Save();
            return true;
        }

        /// <summary>
        /// Kiểm tra milestone điểm nhiệm vụ đã nhận hay chưa.
        /// </summary>
        public bool IsMissionMilestoneClaimed(int milestoneId)
        {
            return _storage.Data.claimedMissionMilestones.Contains(milestoneId);
        }

        /// <summary>
        /// Nhận milestone khi tổng điểm nhiệm vụ đạt yêu cầu.
        /// </summary>
        public bool ClaimMissionMilestone(DynamicHeroesGlobalSpecificationsEventBLDailyMissionMilestoneRow row)
        {
            if (row == null ||
                IsMissionMilestoneClaimed(row.milestone) ||
                _storage.Data.missionPoints < row.pointsRequired)
            {
                return false;
            }

            _storage.Data.claimedMissionMilestones.Add(row.milestone);
            _storage.Save();
            return true;
        }

        /// <summary>
        /// Cộng điểm tích lũy triệu hồi của sự kiện.
        /// </summary>
        public bool AddSummonPoints(int value)
        {
            if (value <= 0)
            {
                return false;
            }

            _storage.Data.summonPoints += value;
            _storage.Save();
            return true;
        }

        /// <summary>
        /// Kiểm tra milestone tích lũy triệu hồi đã nhận hay chưa.
        /// </summary>
        public bool IsSummonMilestoneClaimed(int milestoneId)
        {
            return _storage.Data.claimedSummonMilestones.Contains(milestoneId);
        }

        /// <summary>
        /// Nhận milestone khi điểm tích lũy triệu hồi đạt yêu cầu.
        /// </summary>
        public bool ClaimSummonMilestone(DynamicHeroesGlobalSpecificationsEventBLDailyGachaMilestoneRow row)
        {
            if (row == null ||
                IsSummonMilestoneClaimed(row.milestone) ||
                _storage.Data.summonPoints < row.pointsRequired)
            {
                return false;
            }

            _storage.Data.claimedSummonMilestones.Add(row.milestone);
            _storage.Save();
            return true;
        }

        private bool RegisterLoginInternal(DateTime utcNow)
        {
            var data = _storage.Data;

            if (data.LastLoginDate.HasValue &&
                data.LastLoginDate.Value.Date == utcNow.Date)
            {
                return false;
            }

            data.LastLoginDate = utcNow;
            data.loginDay = Math.Min(MAX_LOGIN_DAY, data.loginDay + 1);
            ChangeMissionProgressInternal("LOGIN", 1);
            return true;
        }

        private bool CheckDailyMissionResetInternal(DateTime utcNow)
        {
            var data = _storage.Data;

            if (data.LastMissionResetDate.HasValue &&
                data.LastMissionResetDate.Value.Date == utcNow.Date)
            {
                EnsureMissionStates();
                return false;
            }

            data.LastMissionResetDate = utcNow;

            data.missions = _missions
                .Select(row => new EventLeHoiBangLongMissionState
                {
                    missionId = row.missionId,
                    progress = 0,
                    isClaimed = false,
                })
                .ToList();

            return true;
        }

        private void ChangeMissionProgressInternal(string trigger, int value)
        {
            foreach (var row in _missions.Where(row => row.trigger == trigger))
            {
                var state = GetMissionState(row.missionId);

                if (state is { isClaimed: false, })
                {
                    state.progress = Math.Min(row.target, state.progress + value);
                }
            }
        }

        private void EnsureCollections()
        {
            var data = _storage.Data;
            data.claimedFreeLoginDays ??= new List<int>();
            data.claimedFreeBonusDays ??= new List<int>();
            data.purchasedBonusDays ??= new List<int>();
            data.claimedBonusDays ??= new List<int>();
            data.missions ??= new List<EventLeHoiBangLongMissionState>();
            data.claimedMissionMilestones ??= new List<int>();
            data.claimedSummonMilestones ??= new List<int>();
            EnsureMissionStates();
        }

        private void EnsureMissionStates()
        {
            foreach (var row in _missions)
            {
                if (_storage.Data.missions.All(state => state.missionId != row.missionId))
                {
                    _storage.Data.missions.Add(new EventLeHoiBangLongMissionState
                    {
                        missionId = row.missionId,
                    });
                }
            }
        }

        private static bool IsValidDay(int day)
        {
            return day is >= 1 and <= MAX_LOGIN_DAY;
        }
    }
}