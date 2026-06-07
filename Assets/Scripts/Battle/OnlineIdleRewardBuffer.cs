using System;
using System.Collections.Generic;
using System.Globalization;

namespace Immortal_Switch.Scripts.Reward
{
    public class OnlineIdleRewardBuffer
    {
        private readonly Dictionary<string, double> rewards = new Dictionary<string, double>();

        public void Add(string currencyType, double amount)
        {
            if (string.IsNullOrWhiteSpace(currencyType))
                return;

            if (amount <= 0)
                return;

            if (!rewards.ContainsKey(currencyType))
                rewards[currencyType] = 0d;

            rewards[currencyType] += amount;
        }

        public bool HasAny()
        {
            foreach (var pair in rewards)
            {
                if (pair.Value >= 1d)
                    return true;
            }

            return false;
        }

        public List<RewardAmountDto> BuildDtos()
        {
            List<RewardAmountDto> result = new List<RewardAmountDto>();

            foreach (var pair in rewards)
            {
                double amount = Math.Floor(pair.Value);

                if (amount <= 0)
                    continue;

                result.Add(new RewardAmountDto
                {
                    currencyType = pair.Key,
                    amount = amount.ToString("0", CultureInfo.InvariantCulture)
                });
            }

            return result;
        }

        public void Clear()
        {
            rewards.Clear();
        }
    }
}