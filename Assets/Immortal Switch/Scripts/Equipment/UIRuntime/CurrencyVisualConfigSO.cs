using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Currency;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    [CreateAssetMenu(fileName = "CurrencyVisualConfig", menuName = "ScriptableObjects/Equipment/CurrencyVisualConfig")]
    public class CurrencyVisualConfigSO : ScriptableObject
    {
        public List<CurrencyVisualEntry> Entries = new();

        public Sprite GetIcon(CurrencyType currencyType)
        {
            var entry = Entries.Find(x => x.CurrencyType == currencyType);
            return entry != null ? entry.Icon : null;
        }
    }

    [Serializable]
    public class CurrencyVisualEntry
    {
        public CurrencyType CurrencyType;
        public Sprite Icon;
    }
}