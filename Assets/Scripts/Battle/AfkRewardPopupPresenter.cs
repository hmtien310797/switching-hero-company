using System.Collections.Generic;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Views;
using UnityEngine;

namespace Immortal_Switch.Scripts.Reward
{
    // Chuyển response.Rewards của afk/claim thành ItemData rồi hiện qua PopupRewardService —
    // dùng chung cho MỌI nơi thực sự commit afk/claim (TopMainView khi player bấm Claim trong
    // AFKRewardView, và OfflineAfkRewardService khi tự động claim lúc resume từ background),
    // để không có đường claim nào lặng lẽ không hiện gì cho player thấy.
    public static class AfkRewardPopupPresenter
    {
        public static void ShowClaimedRewardPopup(List<RewardDto> rewards)
        {
            if (rewards == null ||
                rewards.Count == 0)
                return;

            var itemRewards = new List<ItemData>();

            foreach (var r in rewards)
            {
                if (!BigNumber.TryParse(r.Amount, out var amount) ||
                    amount <= BigNumber.Zero)
                    continue;

                // Resolve item_id số trước, giống mọi chỗ show PopupRewardService khác
                // (SummonRewardReceiver/SettingManager) — tránh currency_type ("gold"/"diamond"...)
                // không match được item và bị skip lặng lẽ.
                var itemRow = DatabaseManager.Instance.ItemDb.FindItem(r.CurrencyType);

                if (itemRow == null)
                {
                    Debug.LogWarning($"[AfkRewardPopupPresenter] AFK reward currency_type '{r.CurrencyType}' not found in ItemDb.");
                    continue;
                }

                itemRewards.Add(new ItemData(itemRow.itemId, amount));
            }

            if (itemRewards.Count > 0)
                PopupRewardService.Show(itemRewards);
        }
    }
}
