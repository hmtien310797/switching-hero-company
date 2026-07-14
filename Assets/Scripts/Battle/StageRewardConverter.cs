using System;
using System.Collections.Generic;
using System.Globalization;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Level.Stage;

namespace Immortal_Switch.Scripts.Reward
{
    public static class StageRewardConverter
    {
        // Server reward DTOs (currency_type + string amount) → client StageReward, dùng để hiển
        // thị số quà thật đã claim (afk/claim, ...) thay vì rate config phía client.
        public static StageReward[] FromRewardDtos(List<RewardDto> dtos)
        {
            var result = new List<StageReward>();
            if (dtos == null) return result.ToArray();

            for (int i = 0; i < dtos.Count; i++)
            {
                RewardDto dto = dtos[i];
                if (!Enum.TryParse(dto.CurrencyType, true, out CurrencyType type))
                    continue;
                if (!double.TryParse(dto.Amount, NumberStyles.Float, CultureInfo.InvariantCulture, out double amount) || amount <= 0)
                    continue;

                result.Add(new StageReward(type, BigNumber.FromDouble(amount)));
            }

            return result.ToArray();
        }

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