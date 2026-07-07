using System;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using UnityEngine.Serialization;

namespace Immortal_Switch.Scripts.Level.Stage
{
    [Serializable]
    public class StageReward
    {
        [FormerlySerializedAs("ResourceType")] public CurrencyType currencyType;
        public BigNumber Amount;

        public StageReward(CurrencyType currencyType, BigNumber amount)
        {
            this.currencyType = currencyType;
            Amount = amount;
        }

        public bool IsValid => currencyType != CurrencyType.none && Amount > BigNumber.Zero;
    }
}