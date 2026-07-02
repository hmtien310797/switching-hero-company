using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Battle.Dungeon
{
    public sealed partial class DungeonBattleController
    {
        public async UniTask<bool> InitializeAsync(int targetDungeonId, int targetStage)
        {
            if (state != DungeonBattleState.None && state != DungeonBattleState.Ended)
                return false;

            CleanupBattle();

            dungeonId = targetDungeonId;
            stage = Mathf.Max(1, targetStage);
            state = DungeonBattleState.Initializing;
            result = DungeonBattleResult.None;
            battleResult = DungeonBattleResult.None;
            battleEnded = false;
            currentScore = 0L;
            finalRewards = Array.Empty<DungeonRuntimeReward>();

            if (!DungeonStageRuntimeBuilder.TryBuild(
                    database,
                    dungeonId,
                    stage,
                    out runtimeData))
            {
                Debug.LogError($"[Dungeon] Cannot build runtime data. dungeonId={dungeonId}, stage={stage}");
                state = DungeonBattleState.None;
                return false;
            }

            if (dungeonMapController == null)
            {
                Debug.LogError("[Dungeon] DungeonMapController is missing.");
                CleanupBattle();
                return false;
            }

            activeDungeonMapView =
                await dungeonMapController.InitDungeonMapAsync(runtimeData.MapName);

            if (activeDungeonMapView == null)
            {
                Debug.LogError(
                    $"[Dungeon] Cannot initialize map. mapName={runtimeData.MapName}");
                CleanupBattle();
                return false;
            }

            activeEnemyTargetProvider = runtimeData.Mode == DungeonModeType.DefendObjective
                ? defenseTargetProvider
                : heroTargetProvider;

            await SpawnLineupHeroesAsync();

            currentMode = DungeonModeFactory.Create(runtimeData.Mode);
            if (currentMode == null)
            {
                Debug.LogError($"[Dungeon] Cannot create mode. mode={runtimeData.Mode}");
                CleanupBattle();
                return false;
            }

            DungeonModeContext context = new DungeonModeContext(
                runtimeData,
                SpawnEnemyForMode,
                SpawnBossForModeAsync,
                SpawnDamageDummy,
                SpawnDefenseObjective,
                CompleteVictory,
                CompleteDefeat,
                SetScore
            );

            await currentMode.InitializeAsync(context);

            remainingTime = Mathf.Max(0f, runtimeData.TimeLimitSec);
            state = DungeonBattleState.Preparing;
            return true;
        }

        public void BeginBattle()
        {
            if (state != DungeonBattleState.Preparing || currentMode == null)
                return;

            state = DungeonBattleState.Fighting;
            currentMode.Begin();
        }

        private void CompleteBattle(DungeonBattleResult completedResult)
        {
            if (battleEnded || state == DungeonBattleState.Ended)
                return;

            battleEnded = true;
            battleResult = completedResult;
            result = completedResult;
            state = DungeonBattleState.Ended;

            finalRewards = completedResult == DungeonBattleResult.Victory
                ? DungeonRewardCalculator.BuildFinalRewards(runtimeData)
                : Array.Empty<DungeonRuntimeReward>();

            BattleEnded?.Invoke(completedResult);
        }
    }
}
