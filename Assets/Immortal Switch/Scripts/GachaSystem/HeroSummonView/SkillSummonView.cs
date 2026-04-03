using System.Collections.Generic;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.SkillSummon;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SkillSummonView : BaseSummonMainView<SkillSummonPaymentType, SkillSummonResult>
    {
        public override SummonCategory Category => SummonCategory.Skill;

        [Header("Reward Preview")]
        [SerializeField] private SharedSummonLevelRewardPreviewUI levelRewardPreviewUI;
        [SerializeField] private Sprite skillTicketRewardIcon;
        [SerializeField] private Sprite ssSkillRewardIcon;

        [Header("Popup")]
        [SerializeField] private SharedSummonConfirmPopup confirmPopup;
        [SerializeField] private SharedSummonSequencePopup sequencePopup;
        [SerializeField] private SkillSummonProbabilityPopup probabilityPopup;
        [SerializeField] private SharedSummonAchievementRewardView summonAchievementRewardView;

        public override bool HasNotification()
        {
            if (SkillSummonManager.Instance == null || SkillSummonManager.Instance.Service == null)
                return false;

            var claimables = SkillSummonManager.Instance.Service.GetClaimableRewardLevels();
            return claimables != null && claimables.Count > 0;
        }

        protected override void SubscribeEvents()
        {
            if (SkillSummonManager.Instance != null)
                SkillSummonManager.Instance.OnSummonDataChanged += RefreshView;

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnAnyCurrencyChanged += RefreshView;
        }

        protected override void UnsubscribeEvents()
        {
            if (SkillSummonManager.Instance != null)
                SkillSummonManager.Instance.OnSummonDataChanged -= RefreshView;

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnAnyCurrencyChanged -= RefreshView;
        }

        protected override bool CanRefresh()
        {
            return SkillSummonManager.Instance != null && SkillSummonManager.Instance.Service != null;
        }

        protected override int GetCurrentSummonLevel()
        {
            return SkillSummonManager.Instance.GetCurrentSummonLevel();
        }

        protected override int GetCurrentLevelProgressRoll()
        {
            return SkillSummonManager.Instance.Service.GetCurrentLevelProgressRoll();
        }

        protected override int GetCurrentLevelRequiredRoll()
        {
            return SkillSummonManager.Instance.Service.GetCurrentLevelRequiredRoll();
        }

        protected override void RefreshRewardPreview()
        {
            if (levelRewardPreviewUI == null || SkillSummonManager.Instance == null || SkillSummonManager.Instance.Service == null)
                return;

            var preview = SkillSummonManager.Instance.Service.GetRewardPreviewData();
            if (preview == null)
            {
                levelRewardPreviewUI.Bind(null, null);
                return;
            }

            var shared = new SharedSummonRewardPreviewData
            {
                SummonLevel = preview.SummonLevel,
                AmountText = BuildRewardAmountText(preview.RewardItem),
                RewardIcon = GetRewardIcon(preview.RewardItem),
                IsClaimable = preview.IsClaimable,
                IsClaimed = preview.IsClaimed
            };

            levelRewardPreviewUI.Bind(shared, HandleClaimPreviewReward);
        }

        protected override bool CanSummon(string optionId, out SkillSummonPaymentType paymentType, out int paidAmount)
        {
            paymentType = SkillSummonPaymentType.Ticket;
            paidAmount = 0;
            return SkillSummonManager.Instance != null &&
                   SkillSummonManager.Instance.CanSummon(optionId, out paymentType, out paidAmount);
        }

        protected override SkillSummonResult DoExecuteSummon(string optionId, SkillSummonPaymentType paymentType)
        {
            return SkillSummonManager.Instance.ExecuteSummon(optionId, paymentType);
        }

        protected override bool IsGemPayment(SkillSummonPaymentType paymentType)
        {
            return paymentType == SkillSummonPaymentType.Gem;
        }

        protected override SkillSummonPaymentType GetGemPaymentType()
        {
            return SkillSummonPaymentType.Gem;
        }

        protected override bool ShouldSkipGemConfirm()
        {
            return SkillSummonManager.Instance != null &&
                   SkillSummonManager.Instance.SaveData.SkipGemFallbackConfirm;
        }

        protected override bool HasConfirmPopup()
        {
            return confirmPopup != null;
        }

        protected override void ShowConfirmPopup(int gemCost, System.Action onConfirm)
        {
            string message = $"Not enough Skill Tickets.\nThis summon will cost {gemCost} Diamonds.\nConfirm?";
            bool skip = SkillSummonManager.Instance != null && SkillSummonManager.Instance.SaveData.SkipGemFallbackConfirm;

            confirmPopup.Show(message, skip, skipValue =>
            {
                if (SkillSummonManager.Instance != null)
                {
                    SkillSummonManager.Instance.SaveData.SkipGemFallbackConfirm = skipValue;
                    SkillSummonManager.Instance.Save();
                }

                onConfirm?.Invoke();
            });
        }

        protected override void HideAllPopups()
        {
            confirmPopup?.Hide();
            sequencePopup?.Hide();
            probabilityPopup?.Hide();
            summonAchievementRewardView?.Hide();
        }

        protected override void OpenProbabilityPopup()
        {
            if (probabilityPopup == null || SkillSummonManager.Instance == null)
                return;

            int currentLevel = SkillSummonManager.Instance.GetCurrentSummonLevel();
            probabilityPopup.Show(currentLevel);
        }

        protected override void OpenAchievementPopup()
        {
            if (summonAchievementRewardView == null || SkillSummonManager.Instance == null || SkillSummonManager.Instance.Config == null)
                return;

            var items = BuildAchievementItems();
            summonAchievementRewardView.Show(items);
        }

        protected override void SetSequenceBusy(bool value)
        {
            sequencePopup?.SetBusyReplacing(value);
        }

        protected override bool IsSequenceShowing()
        {
            return sequencePopup != null && sequencePopup.IsShowing;
        }

        protected override void ReplaceSequenceResult(SkillSummonResult result)
        {
            sequencePopup?.ReplaceResult(BuildSequenceItems(result));
        }

        protected override void ShowSequenceFirstResult(SkillSummonResult result, System.Action<string> summonAction, string optionId)
        {
            sequencePopup?.ShowFirstResult(BuildSequenceItems(result), summonAction, optionId);
        }

        private void HandleClaimPreviewReward()
        {
            if (SkillSummonManager.Instance == null)
                return;

            var preview = SkillSummonManager.Instance.Service.GetRewardPreviewData();
            if (preview == null || !preview.IsClaimable)
                return;

            var receiver = GetComponent<ISkillSummonRewardReceiver>();
            if (receiver == null)
                return;

            bool claimed = SkillSummonManager.Instance.ClaimReward(preview.SummonLevel, receiver);
            if (!claimed)
                return;

            RefreshView();
        }

        private List<SharedSummonSequenceItemData> BuildSequenceItems(SkillSummonResult result)
        {
            var output = new List<SharedSummonSequenceItemData>();
            if (result == null || result.Entries == null)
                return output;

            var grouped = SkillSummonResultGrouper.Group(result);

            for (int i = 0; i < grouped.Count; i++)
            {
                var entry = grouped[i];
                output.Add(new SharedSummonSequenceItemData
                {
                    Icon = entry.SkillAsset != null ? entry.SkillAsset.skillIcon : null,
                    Name = entry.SkillName,
                    AmountText = $"x{entry.Count}",
                    GradeText = entry.Grade.ToString(),
                    IsNew = entry.IsNewSkill
                });
            }

            return output;
        }

        private List<SharedSummonAchievementItemData> BuildAchievementItems()
        {
            var result = new List<SharedSummonAchievementItemData>();
            var config = SkillSummonManager.Instance.Config;
            var save = SkillSummonManager.Instance.SaveData;

            if (config == null || config.LevelRewards == null)
                return result;

            for (int i = 0; i < config.LevelRewards.Count; i++)
            {
                var entry = config.LevelRewards[i];
                if (entry == null || entry.RewardItems == null || entry.RewardItems.Count == 0)
                    continue;

                var reward = entry.RewardItems[0];

                result.Add(new SharedSummonAchievementItemData
                {
                    Title = $"Summon Level {entry.SummonLevel}",
                    RewardText = BuildRewardAmountText(reward),
                    RewardIcon = GetRewardIcon(reward),
                    IsClaimed = save != null && save.ClaimedRewardLevels.Contains(entry.SummonLevel)
                });
            }

            return result;
        }

        private string BuildRewardAmountText(SkillSummonRewardItem reward)
        {
            if (reward == null)
                return string.Empty;

            switch (reward.RewardType)
            {
                case SkillSummonRewardType.Currency:
                    return $"x{reward.Amount}";
                case SkillSummonRewardType.RandomSkill:
                    return $"{reward.RandomSkillGrade} Skill x{reward.Amount}";
                default:
                    return reward.Description;
            }
        }

        private Sprite GetRewardIcon(SkillSummonRewardItem reward)
        {
            if (reward == null)
                return null;

            switch (reward.RewardType)
            {
                case SkillSummonRewardType.Currency:
                    return skillTicketRewardIcon;
                case SkillSummonRewardType.RandomSkill:
                    return ssSkillRewardIcon;
                default:
                    return null;
            }
        }
    }
}