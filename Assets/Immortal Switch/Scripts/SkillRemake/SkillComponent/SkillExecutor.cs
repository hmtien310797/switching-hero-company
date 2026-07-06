using System.Collections.Generic;
using Immortal_Switch.Scripts.Combat;
using UnityEngine;
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
            {
                ExecuteAction(context, phase.Actions[i]);
            }

            if (phase.hasCameraShake)
                GameCameraController.Instance.ShakeCamera();
        }

        public void ExecuteAction(SkillRuntimeContext context, SkillActionData action)
        {
            if (context == null || action == null)
                return;

            if (Random.Range(0f, 100f) > action.ChancePercent)
                return;

            bool isUltimate = context.SkillData.OwnerType == SkillOwnerType.UltimateSkill;

            SkillTargetType targetType = action.TargetTypeOverride;

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
                        context.SkillController.CastSkillImmediately(action.TriggerSkill, isUltimate);
                    break;

                case SkillActionType.ApplyBuff:
                case SkillActionType.ApplyDebuff:
                    Debug.LogWarning(
                        "[SkillExecutor] Buff/Debuff action data is ready, but BuffModule integration is not implemented in phase 1.");
                    break;
            }
        }

        public void ExecuteNestedActions(SkillRuntimeContext context, List<SkillActionData> actions)
        {
            if (actions == null)
                return;

            for (int i = 0; i < actions.Count; i++)
                ExecuteAction(context, actions[i]);
        }

        private float GetScaledValue(
            SkillRuntimeContext context,
            float baseValue,
            bool scaleWithClassSkillLevel)
        {
            if (context == null || context.SkillData == null)
                return baseValue;

            return context.SkillData.GetScaledClassSkillValue(
                baseValue,
                context.SkillLevel,
                scaleWithClassSkillLevel
            );
        }

        private void ExecuteDamage(SkillRuntimeContext context, SkillActionData action, SkillTargetType targetType)
        {
            List<ICombatUnit> targets = targetResolver.ResolveTargets(
                context,
                targetType,
                action.Area
            );

            float effectiveSkillCoefficient = GetScaledValue(
                context,
                action.Damage.SkillDamageBonusPercent,
                action.Damage.ScaleWithClassSkillLevel
            );

            for (int i = 0; i < targets.Count; i++)
            {
                ICombatUnit target = targets[i];

                if (!HasValidTarget(target))
                {
                    continue;
                }
                
                DamageResult damageResult = 
                    DamageCalculator.CalculateDamage(context.Caster, target, effectiveSkillCoefficient);
                target.TakeDamage(damageResult);
                SkillCombatEventReporter.ReportDamageDealt(
                    context.Caster,
                    context.Caster,
                    target,
                    context.SkillData,
                    damageResult
                );
            }
        }

        private bool HasValidTarget(ICombatUnit currentTarget)
        {
            if (!currentTarget.IsUnityAlive())
            {
                return false;
            }
            return !currentTarget.IsDead;
        }

        private void ExecuteDot(SkillRuntimeContext context, SkillActionData action, SkillTargetType targetType)
        {
            List<ICombatUnit> targets = targetResolver.ResolveTargets(context, targetType,action.Area);

            float effectiveDotCoefficient = GetScaledValue(
                context,
                action.Dot.TickDamageBonusPercent,
                action.Dot.ScaleDamageWithClassSkillLevel
            );

            for (int i = 0; i < targets.Count; i++)
            {
                ICombatUnit target = targets[i];
                if (target == null || target.Stats == null || target.Stats.DotModule == null)
                    continue;

                DamageResult result = DamageCalculator.CalculateDamage(
                    context.Caster,
                    target,
                    effectiveDotCoefficient
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

        private void ExecuteSpawnArea(
            SkillRuntimeContext context,
            SkillActionData action)
        {
            if (context == null ||
                action == null ||
                action.Area == null ||
                action.Area.AreaPrefab == null)
            {
                return;
            }

            SkillAreaData areaData = action.Area;

            Vector3 castDirection =
                GetAreaCastDirection(context);

            Vector3 areaOrigin =
                GetAreaOrigin(
                    context,
                    areaData,
                    castDirection);

            Vector3 areaCenter =
                areaData.ResolveAreaCenter(
                    areaOrigin,
                    castDirection);

            Quaternion rotation =
                GetAreaRotation(castDirection);

            SkillAreaRuntime area =
                objectSpawner.Spawn(
                    areaData.AreaPrefab,
                    areaCenter,
                    rotation);

            if (area == null)
                return;

            area.Init(
                context,
                areaData,
                this,
                targetResolver,
                objectSpawner);
        }

        private void ExecuteSpawnProjectile(SkillRuntimeContext context, SkillActionData action)
        {
            // if (action.Projectile == null || action.Projectile.ProjectilePrefab == null)
            //     return;
            //
            // int count = Mathf.Max(1, action.Projectile.Count);
            // for (int i = 0; i < count; i++)
            // {
            //     Vector3 spawnPosition = GetProjectileSpawnPosition(context, action.Projectile);
            //     SkillProjectileRuntime projectile = objectSpawner.Spawn(action.Projectile.ProjectilePrefab, spawnPosition, Quaternion.identity);
            //     if (projectile == null)
            //         continue;
            //
            //     projectile.Init(context, action.Projectile, this, objectSpawner);
            // }
        }

        private Vector3 GetAreaOrigin(
            SkillRuntimeContext context,
            SkillAreaData areaData,
            Vector3 castDirection)
        {
            if (context == null)
                return Vector3.zero;

            switch (areaData.PositionType)
            {
                case SkillAreaPositionType.Self:
                    return context.Caster != null
                        ? context.Caster.Position
                        : context.CastPosition;

                case SkillAreaPositionType.Target:
                    return context.MainTarget != null
                        ? context.MainTarget.Position
                        : context.TargetPosition;

                case SkillAreaPositionType.CastPosition:
                    return context.CastPosition;

                case SkillAreaPositionType.ForwardOffset:
                {
                    Vector3 casterPosition =
                        context.Caster != null
                            ? context.Caster.Position
                            : context.CastPosition;

                    // Giữ behavior cũ:
                    // Radius đang được dùng như khoảng offset phía trước.
                    return casterPosition
                           + castDirection * areaData.Radius;
                }

                default:
                    return context.TargetPosition;
            }
        }

        private Vector3 GetAreaCastDirection(
            SkillRuntimeContext context)
        {
            if (context == null)
                return Vector3.right;

            Vector3 origin =
                context.Caster != null
                    ? context.Caster.Position
                    : context.CastPosition;

            Vector3 destination;

            if (context.MainTarget != null &&
                !context.MainTarget.IsDead)
            {
                destination =
                    context.MainTarget.Position;
            }
            else if ((context.TargetPosition - origin)
                     .sqrMagnitude > 0.0001f)
            {
                destination =
                    context.TargetPosition;
            }
            else
            {
                destination =
                    context.CastPosition;
            }

            Vector3 direction =
                destination - origin;

            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f &&
                context.Caster != null)
            {
                direction =
                    context.Caster.transform.forward;

                direction.y = 0f;
            }

            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector3.right;

            return direction.normalized;
        }

        private Quaternion GetAreaRotation(
            Vector3 castDirection)
        {
            castDirection.y = 0f;

            if (castDirection.sqrMagnitude <= 0.0001f)
                return Quaternion.identity;

            return Quaternion.LookRotation(
                castDirection.normalized,
                Vector3.up);
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
                    return context.Caster != null
                        ? context.Caster.Position + context.Caster.transform.forward * areaData.Radius
                        : context.CastPosition;
                default:
                    return context.TargetPosition;
            }
        }
    }
}