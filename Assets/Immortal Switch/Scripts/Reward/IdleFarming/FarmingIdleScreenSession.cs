using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using UnityEngine;

namespace Immortal_Switch.Scripts.Reward
{
    public class FarmingIdleScreenSession
    {
        private long startUnixTime;
        private int monstersHunted;

        private readonly Dictionary<CurrencyType, BigNumber> earnedRewards = new();

        public bool IsRunning { get; private set; }
        public int MonstersHunted => monstersHunted;

        public void Begin()
        {
            IsRunning = true;
            monstersHunted = 0;
            startUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            earnedRewards.Clear();
        }

        public void End()
        {
            IsRunning = false;
            monstersHunted = 0;
            earnedRewards.Clear();
        }

        public void AddMonsterKill()
        {
            if (!IsRunning)
                return;

            monstersHunted++;
        }

        public void AddEarnedReward(CurrencyType currencyType, BigNumber amount)
        {
            if (!IsRunning)
                return;

            if (amount <= BigNumber.Zero)
                return;

            if (earnedRewards.TryGetValue(currencyType, out BigNumber current))
            {
                earnedRewards[currencyType] = current + amount;
            }
            else
            {
                earnedRewards.Add(currencyType, amount);
            }
        }

        public BigNumber GetEarnedAmount(CurrencyType currencyType)
        {
            if (!IsRunning)
                return BigNumber.Zero;

            return earnedRewards.TryGetValue(currencyType, out BigNumber amount)
                ? amount
                : BigNumber.Zero;
        }

        public int GetElapsedSeconds()
        {
            if (!IsRunning)
                return 0;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Mathf.Max(0, (int)(now - startUnixTime));
        }
    }
}