using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Hero;
using Immortal_Switch.Scripts.Currency;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    [CreateAssetMenu(fileName = "HeroSummonRewardVisualConfig", menuName = "Game/Hero Summon/Reward Visual Config")]
    public class HeroSummonRewardVisualConfigSO : ScriptableObject
    {
        [TableList(AlwaysExpanded = true)]
        public List<RewardVisualEntry> Entries = new();

        public RewardVisualEntry Get(HeroSummonRewardItem item)
        {
            if (item == null)
                return null;

            var exact = Entries.FirstOrDefault(e => e.MatchExact(item));
            if (exact != null)
                return exact;

            return Entries.FirstOrDefault(e => e.MatchByType(item));
        }
    }
    
    [System.Serializable]
    public class RewardVisualEntry
    {
        [HorizontalGroup("Row", 80)]
        [HideLabel]
        public HeroSummonRewardType RewardType;

        [PreviewField(60)]
        public Sprite Icon;

        [LabelWidth(80)]
        public Color Tint = Color.white;

        // ===== Filter =====

        [ShowIf(nameof(IsCurrency))]
        public CurrencyType CurrencyType;

        [ShowIf(nameof(IsHero))]
        public SummonRarity HeroRarity;

        [ShowIf(nameof(IsRandomHero))]
        public SummonRarity RandomHeroRarity;

        // ===== Match logic =====

        public bool MatchExact(HeroSummonRewardItem item)
        {
            if (item.RewardType != RewardType)
                return false;

            switch (RewardType)
            {
                case HeroSummonRewardType.Currency:
                    return item.CurrencyType == CurrencyType;

                case HeroSummonRewardType.Hero:
                    return item.HeroRarity == HeroRarity;

                case HeroSummonRewardType.RandomHero:
                    return item.RandomHeroRarity == RandomHeroRarity;
            }

            return false;
        }

        public bool MatchByType(HeroSummonRewardItem item)
        {
            return item.RewardType == RewardType;
        }
        

        private bool IsCurrency() => RewardType == HeroSummonRewardType.Currency;
        private bool IsHero() => RewardType == HeroSummonRewardType.Hero;
        private bool IsRandomHero() => RewardType == HeroSummonRewardType.RandomHero;
    }
}