using System.Collections.Generic;
using Immortal_Switch.Hero;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class HeroSummonProbabilityViewData
    {
        public int SummonLevel;
        public List<HeroSummonProbabilitySectionData> Sections = new();
    }

    public class HeroSummonProbabilitySectionData
    {
        public SummonRarity Rarity;
        public string RarityLabel;
        public float TotalRatePercent;
        public List<HeroSummonProbabilityHeroData> Heroes = new();
    }

    public class HeroSummonProbabilityHeroData
    {
        public HeroDataSO Hero;
        public float ProbabilityPercent;
    }
}