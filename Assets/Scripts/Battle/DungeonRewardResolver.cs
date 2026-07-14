using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Core;
using UnityEngine;

namespace Battle.Dungeon
{
    public sealed class DungeonRewardResolver
    {
        private static readonly IReadOnlyList<DungeonRuntimeReward>
            EmptyRewards = Array.Empty<DungeonRuntimeReward>();

        private readonly DungeonDatabaseSO dungeonDatabase;

        private readonly List<DungeonRuntimeReward> resolvedRewards =
            new(3);

        public DungeonRewardResolver(
            DungeonDatabaseSO dungeonDatabase)
        {
            this.dungeonDatabase = dungeonDatabase;
        }

        public bool TryGetRewards(
            int dungeonId,
            int stage,
            out IReadOnlyList<DungeonRuntimeReward> rewards)
        {
            rewards = EmptyRewards;
            resolvedRewards.Clear();

            if (dungeonDatabase == null)
            {
                Debug.LogError(
                    "[DungeonRewardResolver] DungeonDatabaseSO is null."
                );

                return false;
            }

            int resolvedStage = Mathf.Max(1, stage);

            if (!dungeonDatabase.TryGetDefinition(
                    dungeonId,
                    out DungeonDefinitionData definition))
            {
                Debug.LogWarning(
                    $"[DungeonRewardResolver] Dungeon definition not found. " +
                    $"DungeonId={dungeonId}"
                );

                return false;
            }

            if (!definition.ContainsStage(resolvedStage))
            {
                Debug.LogWarning(
                    $"[DungeonRewardResolver] Invalid dungeon stage. " +
                    $"DungeonId={dungeonId}, Stage={resolvedStage}, " +
                    $"StageCount={definition.StageCount}"
                );

                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    definition.StageTableKey))
            {
                Debug.LogWarning(
                    $"[DungeonRewardResolver] StageTableKey is empty. " +
                    $"DungeonId={dungeonId}"
                );

                return false;
            }

            DungeonStageFormulaRow formulaRow =
                dungeonDatabase.FindStageFormula(
                    definition.StageTableKey,
                    resolvedStage
                );

            if (formulaRow == null)
            {
                Debug.LogWarning(
                    $"[DungeonRewardResolver] Stage formula not found. " +
                    $"DungeonId={dungeonId}, " +
                    $"TableKey={definition.StageTableKey}, " +
                    $"Stage={resolvedStage}"
                );

                return false;
            }

            AddReward(
                formulaRow.Reward1ItemId,
                formulaRow.Reward1,
                resolvedStage,
                formulaRow.Stage
            );

            AddReward(
                formulaRow.Reward2ItemId,
                formulaRow.Reward2,
                resolvedStage,
                formulaRow.Stage
            );

            AddReward(
                formulaRow.Reward3ItemId,
                formulaRow.Reward3,
                resolvedStage,
                formulaRow.Stage
            );

            if (resolvedRewards.Count == 0)
            {
                return false;
            }

            rewards = resolvedRewards;
            return true;
        }

        public bool TryGetFirstReward(
            int dungeonId,
            int stage,
            out DungeonRuntimeReward reward)
        {
            reward = null;

            if (!TryGetRewards(
                    dungeonId,
                    stage,
                    out IReadOnlyList<DungeonRuntimeReward> rewards))
            {
                return false;
            }

            reward = rewards[0];
            return reward != null;
        }

        private void AddReward(
            string itemId,
            DungeonFormulaData formula,
            int currentStage,
            int formulaStartStage)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            double evaluatedAmount =
                formula.Evaluate(
                    currentStage,
                    formulaStartStage
                );

            if (evaluatedAmount <= 0d)
            {
                return;
            }

            resolvedRewards.Add(new DungeonRuntimeReward(itemId, BigNumber.FromDouble(evaluatedAmount)));
        }
    }
}