using System.Collections.Generic;
using Immortal_Switch.Scripts.Combat;
using UnityEngine;
using Battle;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Skill
{
    public sealed class SkillExecutor
    {
        private readonly SkillTargetResolver targetResolver;
        private readonly ISkillObjectSpawner objectSpawner;

        public SkillExecutor(SkillTargetResolver targetResolver, ISkillObjectSpawner objectSpawner)
        {
            this.targetResolver = targetResolver;
            this.objectSpawner = objectSpawner;
        }

        public void ExecutePhase(SkillRuntimeContext context, SkillPhaseData phase)
        {
            if (context == null || phase == null || phase.Actions == null)
                return;

            for (int i = 0; i < phase.Actions.Count; i++)
                ExecuteAction(context, phase.Actions[i], phase.TargetTypeOverride);
        }

        public void ExecuteAction(SkillRuntimeContext context, SkillActionData action, SkillTargetType inheritedTargetType)
        {
            if (context == null || action == null)
                return;

            if (Random.Range(0f, 100f) > action.ChancePercent)
                return;

            SkillTargetType targetType = action.TargetTypeOverride != SkillTargetType.CurrentTarget
                ? action.TargetTypeOverride
                : inheritedTargetType;

            switch (action.ActionType)
            {
                case SkillActionType.DealDamage:
                    ExecuteDamage(context, action, targetType);
                    break;

                case SkillActionType.ApplyDot:
                    ExecuteDot(context, action, targetType);
                    break;

                case SkillActionType.SpawnArea:
                    ExecuteSpawnArea(context, action);
                    break;

                case SkillActionType.SpawnProjectile:
                    ExecuteSpawnProjectile(context, action);
                    break;

                case SkillActionType.TriggerSkill:
                    if (action.TriggerSkill != null && context.SkillController != null)
                        context.SkillController.CastSkillImmediately(action.TriggerSkill);
                    break;

                case SkillActionType.ApplyBuff:
                case SkillActionType.ApplyDebuff:
                    Debug.LogWarning("[SkillExecutor] Buff/Debuff action data is ready, but BuffModule integration is not implemented in phase 1.");
                    break;
            }
        }

        public void ExecuteNestedActions(SkillRuntimeContext context, List<SkillActionData> actions)
        {
            if (actions == null)
                return;

            for (int i = 0; i < actions.Count; i++)
                ExecuteAction(context, actions[i], SkillTargetType.CurrentTarget);
        }

        private void ExecuteDamage(SkillRuntimeContext context, SkillActionData action, SkillTargetType targetType)
        {
            List<ICombatUnit> targets = targetResolver.ResolveTargets(
                context,
                targetType,
                action.Area
            );
            for (int i = 0; i < targets.Count; i++)
            {
                ICombatUnit target = targets[i];
                DamageResult damageResult =
                    DamageCalculator.CalculateDamage(context.Caster, target, action.Damage.SkillDamageBonusPercent);
                target.TakeDamage(context.Caster, damageResult);
                SkillCombatEventReporter.ReportDamageDealt(
                    context.Caster,
                    context.Caster,
                    target,
                    context.SkillData,
                    damageResult
                );
            }
        }

        private void ExecuteDot(SkillRuntimeContext context, SkillActionData action, SkillTargetType targetType)
        {
            List<ICombatUnit> targets = targetResolver.ResolveTargets(context, targetType);
            for (int i = 0; i < targets.Count; i++)
            {
                ICombatUnit target = targets[i];
                if (target == null || target.Stats == null || target.Stats.DotModule == null)
                    continue;

                DamageResult result = DamageCalculator.CalculateDamage(
                    context.Caster,
                    target,
                    action.Dot.TickDamageBonusPercent
                );

                target.Stats.DotModule.ApplyDotSnapshot(
                    action.Dot.EffectId,
                    context.Caster,
                    target,
                    result.Damage,
                    action.Dot.TickInterval,
                    action.Dot.Duration,
                    action.Dot.DamageType,
                    action.Dot.StackRule
                );
            }
        }

        private void ExecuteSpawnArea(SkillRuntimeContext context, SkillActionData action)
        {
            if (action.Area == null || action.Area.AreaPrefab == null)
                return;

            Vector3 position = GetAreaPosition(context, action.Area);
            SkillAreaRuntime area = objectSpawner.Spawn(action.Area.AreaPrefab, position, Quaternion.identity);
            if (area == null)
                return;

            area.Init(context, action.Area, this, targetResolver, objectSpawner);
        }

        private void ExecuteSpawnProjectile(SkillRuntimeContext context, SkillActionData action)
        {
            if (action.Projectile == null || action.Projectile.ProjectilePrefab == null)
                return;

            int count = Mathf.Max(1, action.Projectile.Count);
            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPosition = GetProjectileSpawnPosition(context, action.Projectile);
                SkillProjectileRuntime projectile = objectSpawner.Spawn(action.Projectile.ProjectilePrefab, spawnPosition, Quaternion.identity);
                if (projectile == null)
                    continue;

                projectile.Init(context, action.Projectile, this, objectSpawner);
            }
        }

        private Vector3 GetAreaPosition(SkillRuntimeContext context, SkillAreaData areaData)
        {
            switch (areaData.PositionType)
            {
                case SkillAreaPositionType.Self:
                    return context.Caster != null ? context.Caster.Position : context.CastPosition;
                case SkillAreaPositionType.Target:
                    return context.MainTarget != null ? context.MainTarget.Position : context.TargetPosition;
                case SkillAreaPositionType.CastPosition:
                    return context.CastPosition;
                case SkillAreaPositionType.ForwardOffset:
                    return context.Caster != null ? context.Caster.Position + context.Caster.transform.forward * areaData.Radius : context.CastPosition;
                default:
                    return context.TargetPosition;
            }
        }

        private Vector3 GetProjectileSpawnPosition(SkillRuntimeContext context, SkillProjectileData projectileData)
        {
            switch (projectileData.SpawnPositionType)
            {
                case SkillSpawnPositionType.Target:
                    return context.MainTarget != null ? context.MainTarget.Position : context.TargetPosition;
                case SkillSpawnPositionType.CastPosition:
                    return context.CastPosition;
                case SkillSpawnPositionType.Self:
                case SkillSpawnPositionType.ProjectileSpawnPoint:
                case SkillSpawnPositionType.CustomSocket:
                default:
                    return context.Caster != null ? context.Caster.Position + Vector3.up * 0.8f : context.CastPosition;
            }
        }
    }
}
