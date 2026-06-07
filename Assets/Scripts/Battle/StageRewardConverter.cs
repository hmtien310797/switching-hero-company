using System;
using System.Collections.Generic;
using System.Globalization;
using Immortal_Switch.Scripts.Level.Stage;

namespace Immortal_Switch.Scripts.Reward
{
    public static class StageRewardConverter
    {
        public static List<RewardAmountDto> ToRewardDtos(StageReward[] rewards)
        {
            List<RewardAmountDto> result = new List<RewardAmountDto>();

            if (rewards == null)
                return result;

            for (int i = 0; i < rewards.Length; i++)
            {
                StageReward reward = rewards[i];

                if (string.IsNullOrWhiteSpace(reward.ResourceType))
                    continue;

                if (reward.Amount <= 0)
                    continue;

                result.Add(new RewardAmountDto
                {
                    currencyType = reward.ResourceType,
                    amount = ToAmountString(reward.Amount)
                });
            }

            return result;
        }

        private static string ToAmountString(double amount)
        {
            // Currency reward nên là số nguyên.
            double floored = Math.Floor(amount);

            return floored.ToString("0", CultureInfo.InvariantCulture);
        }
    }
}