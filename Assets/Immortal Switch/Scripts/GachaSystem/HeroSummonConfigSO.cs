using System;
using System.Collections.Generic;
using Immortal_Switch.Hero;
using UnityEngine;
using UnityEngine.Serialization;

namespace Immortal_Switch.Scripts.GachaSystem
{
    [CreateAssetMenu(fileName = "HeroSummonConfig", menuName = "ScriptableObjects/Summon/HeroSummonConfig")]
    public class HeroSummonConfigSO : ScriptableObject
    {
        public List<HeroSummonOptionEntry> SummonOptions = new();
        public List<HeroSummonLevelEntry> SummonLevels = new();
        public List<HeroSummonLevelRewardEntry> LevelRewards = new();

        [Header("Pity")]
        public bool EnablePity = true;
        public HeroSummonPityMode PityMode = HeroSummonPityMode.SoftHard;
        public SummonRarity PityTargetRarity = SummonRarity.Legendary;
        [Min(0)] public int SoftStart = 40;
        [Min(0)] public int HardPity = 70;
        [Min(0f)] public float SoftBonusPercentPerMiss = 1f;

        [Header("Hero Pool")]
        public List<HeroDataSO> HeroPool = new();

        public HeroSummonOptionEntry GetOption(string optionId)
        {
            return SummonOptions.Find(x => x != null && x.OptionId == optionId);
        }

        public HeroSummonLevelEntry GetLevelEntry(int summonLevel)
        {
            HeroSummonLevelEntry result = null;

            for (int i = 0; i < SummonLevels.Count; i++)
            {
                var entry = SummonLevels[i];
                if (entry == null) continue;

                if (entry.SummonLevel <= summonLevel)
                {
                    if (result == null || entry.SummonLevel > result.SummonLevel)
                        result = entry;
                }
            }

            return result;
        }

        public HeroSummonLevelRewardEntry GetRewardEntry(int summonLevel)
        {
            for (int i = 0; i < LevelRewards.Count; i++)
            {
                var entry = LevelRewards[i];
                if (entry == null) continue;

                if (entry.SummonLevel == summonLevel)
                    return entry;
            }

            return null;
        }
    }

    [Serializable]
    public class HeroSummonOptionEntry
    {
        public string OptionId;
        public string DisplayName;
        [Min(1)] public int RollCount = 1;
        [Min(0)] public int TicketCost = 1;
        [Min(0)] public int GemCost = 1;
        public bool Enabled = true;
    }

    [Serializable]
    public class HeroSummonLevelEntry
    {
        [Min(1)] public int SummonLevel = 1;
        [Min(0)] public int TotalRollRequired = 0;

        [Header("Rates")]
        [Range(0, 100)] public float CommonRate;
        [Range(0, 100)] public float UnCommonRate;
        [Range(0, 100)] public float RareRate;
        [Range(0, 100)] public float EpicRate;
        [Range(0, 100)] public float LegendaryRate;
        [Range(0, 100)] public float MythicRate;
    }

    [Serializable]
    public class HeroSummonLevelRewardEntry
    {
        [Min(1)] public int SummonLevel;
        public List<HeroSummonRewardItem> RewardItems = new();
    }
}