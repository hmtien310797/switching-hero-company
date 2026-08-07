using System.Collections.Generic;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Pvp.Data;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Popup hiển thị reward của 1 tier PvP. Nhận <see cref="PvpRewardViewArgs"/> qua OnShow (title +
    /// rewardStr "3:1000;1:5000"), parse bằng <see cref="DatabaseManager.GetRewards"/> rồi render từng
    /// item bằng <see cref="UIRewardQuantity"/>.
    /// <para>PREFAB (Addressable "PvpRewardView"): txtTitle, rewardContainer (RectTransform), rewardPrefab
    /// (prefab chứa UIRewardQuantity), btnClose.</para>
    /// </summary>
    public class PvpRewardView : BouncePopupUIView
    {
        [SerializeField] private Transform rewardContainer;
        [SerializeField] private PvpItemReward pvpItemReward;
        [SerializeField] private Button btnClose;

        private readonly List<PvpItemReward> pvpSpawnedRewards = new();
        private PvpTierRewardDatabaseSO pvpTierRewardDatabase;

        private void Awake()
        {
            pvpTierRewardDatabase = DatabaseManager.Instance.PvpTierRewardDatabase;
            if (btnClose != null) btnClose.onClick.AddListener(() => UIManager.Instance.Close<PvpRewardView>());
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);
            RenderRewards();
        }

        private void RenderRewards()
        {
            if (rewardContainer == null || pvpItemReward == null) return;

            // Parse "3:1000;1:5000" → List<ItemRewardData> (itemId + quantity).

            for (int i = 0; i < pvpTierRewardDatabase.Rows.Count; i++)
            {
                var currentReward = pvpTierRewardDatabase.Rows[i];
                List<ItemRewardData> rewards = DatabaseManager.Instance?.GetRewards(currentReward.Reward);
                PvpItemReward itemReward = Instantiate(pvpItemReward, rewardContainer);
                itemReward.Bind(null, currentReward.TierLevel.ToString(), LocalizationManager.GetText(currentReward.TierName), currentReward.RequiredPoint.ToString(), rewards.ToArray());
            }
        }
    }
}