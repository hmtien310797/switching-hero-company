using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Hero;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Skill;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    [CreateAssetMenu(fileName = "SummonRewardVisualConfig", menuName = "Game/Summon/Reward Visual Config")]
    public class SummonRewardVisualConfigSO : ScriptableObject
    {
        [TableList(AlwaysExpanded = true)]
        public List<RewardVisualEntry> Entries = new();

        public RewardVisualEntry Get(SummonRewardItem item)
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
        public SummonRewardType RewardType;

        [PreviewField(60)]
        public Sprite Icon;

        [LabelWidth(80)]
        public Color Tint = Color.white;

        public string DisplayName;
        public string Description;

        [ShowIf(nameof(IsCurrency))]
        public CurrencyType CurrencyType;

        [ShowIf(nameof(IsHero))]
        public SummonRarity HeroRarity;

        [ShowIf(nameof(IsRandomHero))]
        public SummonRarity RandomHeroRarity;

        [ShowIf(nameof(IsSkill))]
        public SkillSummonGrade SkillGrade;

        [ShowIf(nameof(IsRandomSkill))]
        public SkillSummonGrade RandomSkillGrade;

        public bool MatchExact(SummonRewardItem item)
        {
            if (item.RewardType != RewardType)
                return false;

            switch (RewardType)
            {
                case SummonRewardType.Currency:
                    return item.CurrencyType == CurrencyType;

                case SummonRewardType.Hero:
                    return item.HeroRarity == HeroRarity;

                case SummonRewardType.RandomHero:
                    return item.RandomHeroRarity == RandomHeroRarity;

                case SummonRewardType.Skill:
                    return item.SkillGrade == SkillGrade;

                case SummonRewardType.RandomSkill:
                    return item.RandomSkillGrade == RandomSkillGrade;
            }

            return false;
        }

        public bool MatchByType(SummonRewardItem item)
        {
            return item.RewardType == RewardType;
        }

        private bool IsCurrency() => RewardType == SummonRewardType.Currency;
        private bool IsHero() => RewardType == SummonRewardType.Hero;
        private bool IsRandomHero() => RewardType == SummonRewardType.RandomHero;
        private bool IsSkill() => RewardType == SummonRewardType.Skill;
        private bool IsRandomSkill() => RewardType == SummonRewardType.RandomSkill;
    }
}