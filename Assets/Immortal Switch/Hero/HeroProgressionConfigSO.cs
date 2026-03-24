using System;
using System.Collections.Generic;
using UnityEngine;

namespace Immortal_Switch.Hero
{
    [CreateAssetMenu(fileName = "HeroProgressionConfig", menuName = "ScriptableObjects/Heroes/HeroProgressionConfig")]
    public class HeroProgressionConfigSO : ScriptableObject
    {
        public HeroDataSO Hero;
        public HeroProgressTier StartingTier = HeroProgressTier.Common;
        [Min(0)] public int StartingStarInTier = 0;

        public List<HeroProgressionNode> Nodes = new();

        public HeroProgressionNode GetNode(HeroProgressTier tier, int starInTier)
        {
            return Nodes.Find(x => x.Tier == tier && x.StarInTier == starInTier);
        }

        public HeroProgressionNode GetNextNode(HeroProgressTier tier, int starInTier)
        {
            var current = GetNode(tier, starInTier);
            if (current == null || current.IsMaxNode) return null;
            return GetNode(current.NextTier, current.NextStarInTier);
        }

        public int GetMaxStarInTier(HeroProgressTier tier)
        {
            int max = -1;
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i].Tier != tier) continue;
                if (Nodes[i].StarInTier > max)
                    max = Nodes[i].StarInTier;
            }

            return Mathf.Max(0, max);
        }
    }

    [Serializable]
    public class HeroProgressionNode
    {
        [Header("Current State")]
        public HeroProgressTier Tier;
        [Min(0)] public int StarInTier = 0;

        [Header("Upgrade To Next")]
        [Min(0)] public int ShardCostToNext = 0;
        public bool IsMaxNode = false;
        public HeroProgressTier NextTier;
        [Min(0)] public int NextStarInTier = 0;

        [Header("Stat Multipliers At This Node")]
        [Min(0.01f)] public float HealthMultiplier = 1f;
        [Min(0.01f)] public float AttackMultiplier = 1f;
        [Min(0.01f)] public float DefenseMultiplier = 1f;
        [Min(0.01f)] public float AccuracyMultiplier = 1f;
        [Min(0.01f)] public float AttackSpeedMultiplier = 1f;
        [Min(0.01f)] public float AttackRangeMultiplier = 1f;
        [Min(0.01f)] public float MoveSpeedMultiplier = 1f;
        [Min(0.01f)] public float CritChanceMultiplier = 1f;
        [Min(0.01f)] public float CritDamageMultiplier = 1f;
    }
}