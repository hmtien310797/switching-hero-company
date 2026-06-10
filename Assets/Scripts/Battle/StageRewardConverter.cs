using System;
using System.Collections.Generic;
using System.Globalization;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Level.Stage;

namespace Immortal_Switch.Scripts.Reward
{
    public static class StageRewardConverter
    {
        public static List<StageReward> ToRewardDtos(StageReward[] rewards)
        {
            List<StageReward> result = new List<StageReward>();

            if (rewards == null)
                return result;

            for (int i = 0; i < rewards.Length; i++)
            {
                StageReward reward = rewards[i];

                if (reward.currencyType == CurrencyType.none)
                    continue;

                if (reward.Amount <= 0)
                    continue;

                result.Add(new StageReward(reward.currencyType, reward.Amount));
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