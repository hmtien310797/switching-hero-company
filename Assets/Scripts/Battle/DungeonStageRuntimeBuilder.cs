using System;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Level.Stage;
using UnityEngine;

namespace Battle.Dungeon
{
    public static class DungeonStageRuntimeBuilder
    {
        public static bool TryBuild(
            DungeonDatabaseSO database,
            int dungeonId,
            int currentStage,
            out DungeonStageRuntimeData runtimeData)
        {
            runtimeData = null;

            if (database == null)
            {
                Debug.LogError("[DungeonRuntimeBuilder] Database is null.");
                return false;
            }

            if (!database.TryGetDefinition(dungeonId, out DungeonDefinitionData definition) ||
                definition == null)
            {
                Debug.LogError($"[DungeonRuntimeBuilder] Missing definition. dungeonId={dungeonId}");
                return false;
            }

            if (!definition.ContainsStage(currentStage))
            {
                Debug.LogError(
                    $"[DungeonRuntimeBuilder] Invalid stage. dungeonId={dungeonId}, " +
                    $"stage={currentStage}, maxStage={definition.StageCount}");
                return false;
            }

            DungeonStageFormulaRow formulaRow =
                database.FindStageFormula(definition.StageTableKey, currentStage);

            if (formulaRow == null)
            {
                Debug.LogError(
                    $"[DungeonRuntimeBuilder] Missing stage formula. " +
                    $"tableKey={definition.StageTableKey}, stage={currentStage}");
                return false;
            }

            int formulaStartStage = formulaRow.Stage;

            float hpMultiplier = ToPositiveFloat(
                formulaRow.EnemyHp.Evaluate(currentStage, formulaStartStage),
                1f);

            float atkMultiplier = ToPositiveFloat(
                formulaRow.EnemyAtk.Evaluate(currentStage, formulaStartStage),
                1f);

            float defMultiplier = ToPositiveFloat(
                formulaRow.EnemyDef.Evaluate(currentStage, formulaStartStage),
                1f);

            StageStatScale combatScale = new StageStatScale
            {
                HpMultiplier = hpMultiplier,
                AtkMultiplier = atkMultiplier,
                DefMultiplier = defMultiplier
            };
            combatScale.Normalize();

            runtimeData = new DungeonStageRuntimeData
            {
                TempDungeonName = definition.UiNameEn,
                DungeonId = definition.DungeonId,
                DungeonKey = definition.DungeonKey,
                MapName = definition.MapName,
                Stage = currentStage,
                Mode = definition.Mode,
                EntryCostKey = definition.EntryCostKey,
                EntryCostAmount = Mathf.Max(0, definition.EntryCostAmount),
                TimeLimitSec = formulaRow.TimeLimitOverrideSec > 0
                    ? formulaRow.TimeLimitOverrideSec
                    : Mathf.Max(1, definition.DefaultTimeLimitSec),
                RecommendedPower = Math.Max(
                    0d,
                    formulaRow.RecommendedPower.Evaluate(currentStage, formulaStartStage)),
                EnemyScale = combatScale,
                BossScale = combatScale,
                EnemyId = definition.EnemyId,
                BossId = definition.BossId,
                TotalEnemyCount = Math.Max(
                    0,
                    ToInt(formulaRow.EnemyCount.Evaluate(currentStage, formulaStartStage))),
                EnemyPerBatch = Mathf.Max(1, formulaRow.EnemyPerBatch),
                DelayBetweenBatchesSec = Mathf.Max(0f, formulaRow.DelayBetweenBatchesSec),
                Rewards = BuildRewards(formulaRow, currentStage, formulaStartStage)
            };

            if (string.IsNullOrWhiteSpace(definition.MapName))
            {
                Debug.LogError(
                    $"[DungeonRuntimeBuilder] Dungeon requires map_name. " +
                    $"dungeon={definition.DungeonKey}");
                runtimeData = null;
                return false;
            }

            if (RequiresEnemy(definition.Mode) && definition.EnemyId <= 0)
            {
                Debug.LogError(
                    $"[DungeonRuntimeBuilder] Dungeon requires a fixed enemy_id. " +
                    $"dungeon={definition.DungeonKey}, mode={definition.Mode}");
                runtimeData = null;
                return false;
            }

            if (definition.Mode == DungeonModeType.BossChallenge &&
                definition.BossId <= 0)
            {
                Debug.LogError(
                    $"[DungeonRuntimeBuilder] BossChallenge requires a fixed boss_id. " +
                    $"dungeon={definition.DungeonKey}");
                runtimeData = null;
                return false;
            }

            if (definition.Mode == DungeonModeType.DamageChallenge)
            {
                if (!TryBuildDamageChallengeData(
                        database,
                        definition,
                        currentStage,
                        out DungeonDamageChallengeRuntimeData damageData))
                {
                    runtimeData = null;
                    return false;
                }

                runtimeData.DamageChallenge = damageData;
            }

            return true;
        }

        private static bool RequiresEnemy(DungeonModeType mode)
        {
            return mode == DungeonModeType.KillAllEnemies ||
                   mode == DungeonModeType.DefendObjective;
        }

        private static DungeonRuntimeReward[] BuildRewards(
            DungeonStageFormulaRow formulaRow,
            int currentStage,
            int formulaStartStage)
        {
            string[] itemIds =
            {
                formulaRow.Reward1ItemId,
                formulaRow.Reward2ItemId,
                formulaRow.Reward3ItemId
            };

            DungeonFormulaData[] formulas =
            {
                formulaRow.Reward1,
                formulaRow.Reward2,
                formulaRow.Reward3
            };

            DungeonRuntimeReward[] temp = new DungeonRuntimeReward[3];
            int validCount = 0;

            for (int i = 0; i < itemIds.Length; i++)
            {
                string itemId = itemIds[i];
                if (string.IsNullOrEmpty(itemId))
                    continue;

                double amount = Math.Max(
                    0d,
                    formulas[i].Evaluate(currentStage, formulaStartStage));

                if (amount <= 0d)
                    continue;

                temp[validCount++] = new DungeonRuntimeReward(itemId, BigNumber.FromDouble(amount));
            }

            if (validCount == 0)
                return Array.Empty<DungeonRuntimeReward>();

            if (validCount == temp.Length)
                return temp;

            DungeonRuntimeReward[] result = new DungeonRuntimeReward[validCount];
            Array.Copy(temp, result, validCount);
            return result;
        }

        private static bool TryBuildDamageChallengeData(
            DungeonDatabaseSO database,
            DungeonDefinitionData definition,
            int currentStage,
            out DungeonDamageChallengeRuntimeData damageData)
        {
            damageData = null;

            DungeonDamageThresholdRow row =
                database.FindDamageThresholdFormula(definition.StageTableKey, currentStage);

            if (row == null)
            {
                Debug.LogError(
                    $"[DungeonRuntimeBuilder] Missing Damage Challenge required damage formula. " +
                    $"tableKey={definition.StageTableKey}, stage={currentStage}");
                return false;
            }

            damageData = new DungeonDamageChallengeRuntimeData
            {
                RequiredDamage = Math.Max(
                    0d,
                    row.RequiredDamage.Evaluate(currentStage, row.Stage)),
                RewardMultiplierPercent = Math.Max(0f, row.RewardMultiplierPercent)
            };

            return true;
        }

        private static int ToInt(double value)
        {
            if (value <= int.MinValue)
                return int.MinValue;
            if (value >= int.MaxValue)
                return int.MaxValue;

            return Convert.ToInt32(
                Math.Round(value, MidpointRounding.AwayFromZero));
        }

        private static float ToPositiveFloat(double value, float fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                return fallback;

            return value >= float.MaxValue ? float.MaxValue : (float)value;
        }
    }
}
