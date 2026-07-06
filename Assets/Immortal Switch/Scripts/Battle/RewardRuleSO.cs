using System;
using Immortal_Switch.Scripts.Currency;
using UnityEngine;

namespace Immortal_Switch.Scripts.Level.Stage
{
    [CreateAssetMenu(fileName = "RewardRule", menuName = "ScriptableObjects/Stage/RewardRule")]
    public class RewardRuleSO : ScriptableObject
    {
        public RewardRule[] Rules;
    }

    [Serializable]
    public class RewardRule
    {
        public string RewardRuleId;

        public RewardFormulaEntry[] BaseRewards;
        public RewardFormulaEntry[] ClearRewards;

        [TextArea]
        public string Note;
    }

    [Serializable]
    public class RewardFormulaEntry
    {
        public CurrencyType ResourceType;
        public string Formula;
    }
}