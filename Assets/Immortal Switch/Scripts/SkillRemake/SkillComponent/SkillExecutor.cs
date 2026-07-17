using System.Collections.Generic;
using Common;
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

            bool isUltimate = context.SkillData != null && context.SkillData.OwnerType == SkillOwnerType.UltimateSkill;

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
                    ExecuteBuffDebuff(context, action);
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


        private readonly List<ICombatUnit> buffTargetBuffer = new();

        public bool IsWhileInAreaBuffAction(SkillActionData action)
        {
            return action != null &&
                   (action.ActionType == SkillActionType.ApplyBuff ||
                    action.ActionType == SkillActionType.ApplyDebuff) &&
                   action.StatModifier != null &&
                   action.StatModifier.ApplyMode == SkillBuffApplyMode.WhileInArea;
        }

        private void ExecuteBuffDebuff(
            SkillRuntimeContext context,
            SkillActionData action)
        {
            if (context == null || action == null || action.StatModifier == null)
                return;

            // WhileInArea phải do SkillAreaRuntime quản lý để remove đúng lúc target rời vùng.
            if (action.StatModifier.ApplyMode == SkillBuffApplyMode.WhileInArea)
                return;

            ResolveBuffTargets(context, action, action.Area, buffTargetBuffer, onlyTargetsInsideArea: false);

            BuffKind kind = action.ActionType == SkillActionType.ApplyDebuff
                ? BuffKind.Debuff
                : BuffKind.Buff;

            for (int i = 0; i < buffTargetBuffer.Count; i++)
            {
                ICombatUnit target = buffTargetBuffer[i];
                if (!HasValidTarget(target) || target.Stats == null || target.Stats.BuffModule == null)
                    continue;

                BuffData buffData = CreateBuffData(context, action, kind, string.Empty);
                target.Stats.BuffModule.ApplyBuff(buffData);
            }

            buffTargetBuffer.Clear();
        }

        public BuffData CreateBuffData(
            SkillRuntimeContext context,
            SkillActionData action,
            BuffKind kind,
            string idSuffix)
        {
            SkillStatModifierData modifierData = action != null ? action.StatModifier : null;
            if (modifierData == null)
                return null;

            float value = GetScaledValue(
                context,
                modifierData.PercentValue,
                modifierData.ScaleValueWithClassSkillLevel);

            string skillKey = context != null && context.SkillData != null
                ? context.SkillData.SkillKey
                : "unknown_skill";

            string modifierId = string.IsNullOrEmpty(modifierData.ModifierId)
                ? "skill_stat_modifier"
                : modifierData.ModifierId;

            string suffix = string.IsNullOrEmpty(idSuffix)
                ? string.Empty
                : $"_{idSuffix}";

            BuffData buffData = new BuffData
            {
                Id = $"{skillKey}_{modifierId}{suffix}",
                Name = modifierId,
                Kind = kind,
                Duration = Mathf.Max(0.01f, modifierData.Duration),
                MaxStacks = Mathf.Max(1, modifierData.MaxStacks),
                StackRule = modifierData.StackRule,
                Modifiers = new List<StatModifier>
                {
                    new StatModifier(
                        modifierData.StatType,
                        modifierData.Operation,
                        value)
                }
            };

            return buffData;
        }

        public void ResolveBuffTargets(
            SkillRuntimeContext context,
            SkillActionData action,
            SkillAreaData areaData,
            List<ICombatUnit> results,
            bool onlyTargetsInsideArea)
        {
            results.Clear();

            if (context == null || action == null || action.StatModifier == null)
                return;

            SkillBuffTargetMask mask = action.StatModifier.TargetMask;

            if ((mask & SkillBuffTargetMask.Self) != 0)
                AddBuffTarget(context.Caster, results);

            if ((mask & SkillBuffTargetMask.Allies) != 0)
                ResolveAllyTargets(results);

            if ((mask & SkillBuffTargetMask.Enemies) != 0)
            {
                SkillTargetType targetType = onlyTargetsInsideArea
                    ? SkillTargetType.AreaAroundTarget
                    : action.TargetTypeOverride;

                List<ICombatUnit> enemies = targetResolver.ResolveTargets(context, targetType, areaData);
                for (int i = 0; i < enemies.Count; i++)
                    AddBuffTarget(enemies[i], results);
            }

            if (!onlyTargetsInsideArea || areaData == null)
                return;

            Vector3 center = context.TargetPosition;
            Vector3 direction = ResolveAreaDirection(context);

            for (int i = results.Count - 1; i >= 0; i--)
            {
                if (!IsUnitInsideArea(results[i], areaData, center, direction))
                    results.RemoveAt(i);
            }
        }

        private void ResolveAllyTargets(List<ICombatUnit> results)
        {
            UserDataCache cache = UserDataCache.Instance;
            if (cache == null || cache.inBattleHeroes == null)
                return;

            for (int i = 0; i < cache.inBattleHeroes.Length; i++)
                AddBuffTarget(cache.inBattleHeroes[i], results);
        }

        private void AddBuffTarget(ICombatUnit target, List<ICombatUnit> results)
        {
            if (!HasValidTarget(target))
                return;

            if (!results.Contains(target))
                results.Add(target);
        }

        private static Vector3 ResolveAreaDirection(SkillRuntimeContext context)
        {
            if (context == null || context.Caster == null)
                return Vector3.right;

            Vector3 direction = context.TargetPosition - context.Caster.Position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = context.Caster.Transform != null
                    ? context.Caster.Transform.forward
                    : Vector3.right;

            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector3.right;

            return direction.normalized;
        }

        public bool IsUnitInsideArea(
            ICombatUnit unit,
            SkillAreaData areaData,
            Vector3 areaCenter,
            Vector3 castDirection)
        {
            if (!HasValidTarget(unit) || areaData == null)
                return false;

            Vector3 unitPosition = unit.Position;
            unitPosition.y = areaCenter.y;

            switch (areaData.Shape)
            {
                case SkillAreaShape.Box:
                    return IsInsideBox(unitPosition, areaCenter, castDirection, areaData.BoxLength, areaData.BoxWidth);

                case SkillAreaShape.Circle:
                default:
                    float radius = Mathf.Max(0f, areaData.Radius);
                    return (unitPosition - areaCenter).sqrMagnitude <= radius * radius;
            }
        }

        private static bool IsInsideBox(
            Vector3 targetPosition,
            Vector3 boxCenter,
            Vector3 forwardDirection,
            float boxLength,
            float boxWidth)
        {
            forwardDirection.y = 0f;

            if (forwardDirection.sqrMagnitude <= 0.0001f)
                forwardDirection = Vector3.right;

            forwardDirection.Normalize();

            Vector3 rightDirection = new(
                -forwardDirection.z,
                0f,
                forwardDirection.x);

            Vector3 offset = targetPosition - boxCenter;
            offset.y = 0f;

            float forwardDistance = Vector3.Dot(offset, forwardDirection);
            float sideDistance = Vector3.Dot(offset, rightDirection);
            float halfLength = Mathf.Max(0f, boxLength) * 0.5f;
            float halfWidth = Mathf.Max(0f, boxWidth) * 0.5f;

            return Mathf.Abs(forwardDistance) <= halfLength &&
                   Mathf.Abs(sideDistance) <= halfWidth;
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
            if (currentTarget == null || !currentTarget.IsUnityAlive())
                return false;

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
                action.Area == null)
            {
                return;
            }

            SkillAreaData areaData = action.Area;
            
            context.RuntimeObject.skillArea.Init(
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