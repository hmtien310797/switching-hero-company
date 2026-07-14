using System;

namespace Battle.Dungeon
{
    public static class DungeonRewardCalculator
    {
        public static DungeonRuntimeReward[] BuildFinalRewards(
            DungeonStageRuntimeData runtimeData)
        {
            if (runtimeData == null || runtimeData.Rewards == null)
            {
                return Array.Empty<DungeonRuntimeReward>();
            }

            double multiplier = 1d;

            if (runtimeData.Mode == DungeonModeType.DamageChallenge)
            {
                if (runtimeData.DamageChallenge == null)
                {
                    return Array.Empty<DungeonRuntimeReward>();
                }

                multiplier = Math.Max(
                    0d,
                    runtimeData.DamageChallenge.RewardMultiplierPercent / 100d
                );
            }

            DungeonRuntimeReward[] result =
                new DungeonRuntimeReward[runtimeData.Rewards.Length];

            for (int i = 0; i < runtimeData.Rewards.Length; i++)
            {
                DungeonRuntimeReward source = runtimeData.Rewards[i];

                result[i] = new DungeonRuntimeReward(source.ItemKey, source.Quantity * multiplier);
            }

            return result;
        }
    }
}
