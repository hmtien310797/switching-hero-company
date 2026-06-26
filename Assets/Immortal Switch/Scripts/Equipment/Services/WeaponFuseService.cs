using UnityEngine;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.Definitions;
using Immortal_Switch.Scripts.Equipment.Models;

namespace Immortal_Switch.Scripts.Equipment.Services
{
    public class WeaponFuseService
    {
        private readonly WeaponDatabaseSO database;
        private readonly WeaponInventoryService inventory;

        public WeaponFuseService(WeaponDatabaseSO database, WeaponInventoryService inventory)
        {
            this.database = database;
            this.inventory = inventory;
        }

        public WeaponFuseResult TryFuseStandard(int sourceWeaponId)
        {
            var result = new WeaponFuseResult
            {
                SourceWeaponId = sourceWeaponId,
                Success = false
            };

            var sourceDef = database.GetStandard(sourceWeaponId);
            if (sourceDef == null)
                return result;

            var sourceState = inventory.GetOrCreateStandardState(sourceWeaponId);
            if (!sourceState.IsUnlocked)
                return result;

            if (sourceState.CurrentShard < sourceDef.FuseShardRequired)
                return result;

            switch (sourceDef.FuseMode)
            {
                case WeaponFuseMode.ToNextStandard:
                    return FuseToNextStandard(sourceDef, sourceState, result);

                case WeaponFuseMode.ToRandomExclusive:
                    return FuseToRandomExclusive(sourceDef, sourceState, result);

                default:
                    return result;
            }
        }

        private WeaponFuseResult FuseToNextStandard(
            StandardWeaponDefinitionSO sourceDef,
            StandardWeaponState sourceState,
            WeaponFuseResult result)
        {
            var targetDef = database.GetStandard(sourceDef.NextWeaponId);
            if (targetDef == null)
                return result;

            sourceState.CurrentShard -= sourceDef.FuseShardRequired;

            var targetState = inventory.GetOrCreateStandardState(targetDef.WeaponId);
            if (!targetState.IsUnlocked)
            {
                targetState.IsUnlocked = true;
                targetState.Level = 1;
                targetState.LimitBreakStage = 0;
                targetState.CurrentShard = 0;

                result.Success = true;
                result.UnlockedNewStandard = true;
                result.TargetStandardWeaponId = targetDef.WeaponId;
                return result;
            }

            targetState.CurrentShard += 1;

            result.Success = true;
            result.AddedShardToExistingStandard = true;
            result.TargetStandardWeaponId = targetDef.WeaponId;
            return result;
        }

        private WeaponFuseResult FuseToRandomExclusive(
            StandardWeaponDefinitionSO sourceDef,
            StandardWeaponState sourceState,
            WeaponFuseResult result)
        {
            var pool = database.GetExclusivesByClass(sourceDef.ExclusivePoolClass);
            if (pool == null || pool.Count == 0)
                return result;

            CurrencyType classStoneType = WeaponCurrencyHelper.GetClassStoneCurrency(sourceDef.ExclusivePoolClass);
            if(!CurrencyLedgerService.Instance.TrySpend(classStoneType, sourceDef.ExclusiveClassStoneCost,
                CurrencyTransactionReason.FuseWeapon))
            {
                return result;
            }

            sourceState.CurrentShard -= sourceDef.FuseShardRequired;

            int randomIndex = Random.Range(0, pool.Count);
            var targetDef = pool[randomIndex];

            var targetState = inventory.GetOrCreateExclusiveState(targetDef.ExclusiveWeaponId, targetDef.HeroId);
            if (!targetState.IsUnlocked)
            {
                targetState.IsUnlocked = true;
                targetState.Level = 1;
                targetState.LimitBreakStage = 0;
                targetState.CurrentShard = 0;

                result.Success = true;
                result.UnlockedNewExclusive = true;
                result.TargetExclusiveWeaponId = targetDef.ExclusiveWeaponId;
                result.TargetHeroId = targetDef.HeroId;
                return result;
            }

            targetState.CurrentShard += 1;

            result.Success = true;
            result.AddedShardToExistingExclusive = true;
            result.TargetExclusiveWeaponId = targetDef.ExclusiveWeaponId;
            result.TargetHeroId = targetDef.HeroId;
            return result;
        }
    }
}