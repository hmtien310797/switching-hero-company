using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shop.Views;
using Immortal_Switch.Scripts.Shop.Views.UI;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shop.Layouts
{
    public class UIShopGloryPassLayout : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtTime;

        [SerializeField]
        private Transform gloryContainer;

        [SerializeField]
        private UIShopGloryItem gloryPrefab;

        // --- Private Fields ---
        private List<UIShopGloryItem> _glories = new();
        private List<DynamicHeroesGlobalSpecificationsGameRechargeMilestoneRow> _rows = new();
        private CancellationTokenSource _cts;

        private Action<int, EShopTab> _onClickClaim;
        private Action<EShopTab> _onChangeTab;
        private int _lastMonthKey; // YYYYMM

        private void OnEnable()
        {
            ShopManager.Instance.OnDataChanged += OnShopDataChanged;
            StartCountdown().Forget();
        }

        private void OnDisable()
        {
            ShopManager.Instance.OnDataChanged -= OnShopDataChanged;
            _cts?.Cancel();
        }

        private async UniTaskVoid StartCountdown()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            var token = _cts.Token;
            var displayCounter = 0;

            // Update ngay khi vào UI
            CheckMonthChange();
            UpdateTimeDisplay();

            while (!token.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromMinutes(1), cancellationToken: token);

                CheckMonthChange();

                displayCounter++;

                if (displayCounter >= 60)
                {
                    displayCounter = 0;
                    UpdateTimeDisplay();
                }
            }
        }

        private void CheckMonthChange()
        {
            var now = DateTime.UtcNow;
            var currentMonthKey = now.Year * 100 + now.Month;

            if (currentMonthKey != _lastMonthKey)
            {
                _lastMonthKey = currentMonthKey;
                ShopManager.Instance.ResetGloryPassClaims();

                if (_rows.Count > 0)
                {
                    RefreshProducts(_rows);
                }
            }
        }

        private void UpdateTimeDisplay()
        {
            var now = DateTime.UtcNow;
            var endOfMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
            var remaining = endOfMonth - now;
            txtTime.text = $"Còn: {((int)remaining.TotalDays):00} ngày {remaining.Hours:00} giờ";
        }

        private void OnShopDataChanged()
        {
            if (_rows.Count > 0)
            {
                RefreshProducts(_rows);
            }
        }

        public void Bind(List<DynamicHeroesGlobalSpecificationsGameRechargeMilestoneRow> rows, Action<EShopTab> onChangeTab,
            Action<int, EShopTab> onClickClaim)
        {
            _onClickClaim = onClickClaim;
            _onChangeTab = onChangeTab;
            _rows = rows;

            RefreshProducts(rows);
        }

        private void RefreshProducts(List<DynamicHeroesGlobalSpecificationsGameRechargeMilestoneRow> rows)
        {
            var topupCount = ShopManager.Instance.GetTopupCount();

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rewards = DatabaseManager.Instance.GetShopGloryPassRewards(row.iD);
                var isClaimed = ShopManager.Instance.IsGloryPassClaimed(row.iD);

                if (_glories.Count > i)
                {
                    var clone = _glories[i];
                    clone.gameObject.SetActive(true);
                    clone.Bind(topupCount, row.requirePoints, isClaimed, row.nameVi, row.iD, rewards, OnClickClaim);
                }
                else
                {
                    var clone = Instantiate(gloryPrefab, gloryContainer);
                    clone.Bind(topupCount, row.requirePoints, isClaimed, row.nameVi, row.iD, rewards, OnClickClaim);
                    _glories.Add(clone);
                }
            }
        }

        private void OnClickClaim(bool shouldClaim, int shopPackId)
        {
            if (shouldClaim)
            {
                _onClickClaim?.Invoke(shopPackId, EShopTab.GloryPass);
            }
            else
            {
                _onChangeTab?.Invoke(EShopTab.Topup);
            }
        }
    }
}