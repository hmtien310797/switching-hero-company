using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.AFKReward.Interfaces;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.PlayerSystem;
using Immortal_Switch.Scripts.Shared.Constants;
using UnityEngine;

namespace Immortal_Switch.Scripts.AFKReward
{
    /// <summary>
    /// Quản lý số lượt claim x2 AFK Reward mỗi ngày.
    /// Tự động reset khi sang ngày mới.
    /// </summary>
    public class AFKRewardManager : Singleton<AFKRewardManager>
    {
        private IAFKRewardStorage _storage;

        protected override void OnSingletonAwake()
        {
            _storage = new AFKRewardStorage();
            _storage.Load();
            CheckDailyReset();
            PlayerSystemManager.Instance.OnLoginNewDay += OnLoginNewDay;
        }

        protected override void OnDestroy()
        {
            PlayerSystemManager.Instance.OnLoginNewDay -= OnLoginNewDay;
            base.OnDestroy();
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        private void OnLoginNewDay()
        {
            CheckDailyReset();
        }

        /// <summary>
        /// Số lượt xem ads còn lại hôm nay.
        /// </summary>
        public int GetRemainingAds()
        {
            CheckDailyReset();
            return Mathf.Max(0, ValueConstants.MAX_ADS_COUNT - _storage.Data.ClaimX2Count);
        }

        /// <summary>
        /// Tăng biến đếm claim x2, trả về false nếu đã hết lượt.
        /// </summary>
        public bool RecordClaimX2()
        {
            CheckDailyReset();

            if (_storage.Data.ClaimX2Count >= ValueConstants.MAX_ADS_COUNT)
            {
                Debug.LogWarning("[AFKRewardManager] Đã hết lượt claim x2 hôm nay.");
                return false;
            }

            _storage.Data.ClaimX2Count++;
            _storage.Data.ClaimX2Date = DateTime.UtcNow;
            _storage.Save();
            return true;
        }

        /// <summary>
        /// Reset số lượt claim nếu đã sang ngày mới.
        /// </summary>
        private void CheckDailyReset()
        {
            if (_storage.Data.ClaimX2Date == null)
            {
                return;
            }

            var lastDate = _storage.Data.ClaimX2Date.Value.Date;
            var today = DateTime.UtcNow.Date;

            if (lastDate < today)
            {
                _storage.Data.ClaimX2Count = 0;
                _storage.Save();
            }
        }
    }
}