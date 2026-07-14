using System;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Items.ScriptableObjects;
using Immortal_Switch.Scripts.Level.Stage;
using UnityEngine;

namespace Battle.Dungeon
{
    [Serializable]
    public class DungeonRuntimeReward : ItemRewardData
    {
        public DungeonRuntimeReward(string itemKey, BigNumber quantity) : base(itemKey, quantity)
        {
        }

        public DungeonRuntimeReward(string itemKey, BigNumber quantity, Sprite itemIcon, ItemTierEntry tierInfo) : base(itemKey, quantity, itemIcon, tierInfo)
        {
        }
    }

    [Serializable]
    public sealed class DungeonDamageChallengeRuntimeData
    {
        public double RequiredDamage;
        public float RewardMultiplierPercent = 100f;

        public bool IsCleared(double totalDamage)
        {
            return totalDamage >= RequiredDamage;
        }
    }

    [Serializable]
    public sealed class DungeonStageRuntimeData
    {
        public string TempDungeonName;
        public int DungeonId;
        public string DungeonKey;
        public string MapName;
        public int Stage;
        public DungeonModeType Mode;

        public string EntryCostKey;
        public int EntryCostAmount;
        public float TimeLimitSec;
        public double RecommendedPower;

        public StageStatScale EnemyScale;
        public StageStatScale BossScale;

        public int EnemyId;
        public int BossId;

        public int TotalEnemyCount;
        public int EnemyPerBatch;
        public float DelayBetweenBatchesSec;

        public DungeonRuntimeReward[] Rewards;
        public DungeonDamageChallengeRuntimeData DamageChallenge;
    }
}
