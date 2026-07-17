using System.Collections.Generic;
using Common;
using Battle;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    public sealed class SkillTargetResolver
    {
        private readonly List<ICombatUnit> buffer = new();

        public ICombatUnit ResolveMainTarget(
            SkillRuntimeContext context,
            SkillTargetSelectType selectType)
        {
            if (context == null)
                return null;

            switch (selectType)
            {
                case SkillTargetSelectType.Self:
                    return context.Caster;

                case SkillTargetSelectType.CurrentTarget:
                    if (context.Caster != null && context.Caster.HasValidTarget())
                        return context.Caster.CurrentTarget;

                    return context.BattleContext != null && context.Caster != null
                        ? context.BattleContext.GetNearestEnemy(context.Caster.Position)
                        : null;

                case SkillTargetSelectType.NearestEnemy:
                default:
                    return context.BattleContext != null && context.Caster != null
                        ? context.BattleContext.GetNearestEnemy(context.Caster.Position)
                        : null;
            }
        }

        public List<ICombatUnit> ResolveTargets(
            SkillRuntimeContext context,
            SkillTargetType targetType,
            SkillAreaData areaData = null)
        {
            buffer.Clear();

            if (context == null)
                return buffer;

            switch (targetType)
            {
                case SkillTargetType.Self:
                    AddIfValid(context.Caster);
                    break;

                case SkillTargetType.CurrentTarget:
                    AddIfValid(context.MainTarget);
                    break;

                case SkillTargetType.NearestEnemy:
                    AddIfValid(
                        context.BattleContext != null && context.Caster != null
                            ? context.BattleContext.GetNearestEnemy(context.Caster.Position)
                            : null);
                    break;

                case SkillTargetType.AreaAroundSelf:
                {
                    Vector3 origin = context.Caster != null
                        ? context.Caster.Position
                        : context.CastPosition;

                    ResolveEnemiesInArea(context, origin, areaData);
                    break;
                }

                case SkillTargetType.AreaAroundTarget:
                {
                    Vector3 origin = context.MainTarget != null
                        ? context.MainTarget.Position
                        : context.TargetPosition;

                    ResolveEnemiesInArea(context, origin, areaData);
                    break;
                }

                case SkillTargetType.AreaAroundCastPosition:
                    ResolveEnemiesInArea(
                        context,
                        context.RuntimeObject != null
                            ? context.RuntimeObject.transform.position
                            : context.CastPosition,
                        areaData);
                    break;

                case SkillTargetType.AllEnemies:
                    ResolveAllEnemies(context);
                    break;

                case SkillTargetType.AllAllies:
                    ResolveAllAllies();
                    break;
            }

            return buffer;
        }

        public int CountEnemiesInRange(
            SkillRuntimeContext context,
            Vector3 center,
            float range)
        {
            IBattleTargetRegistry registry = context?.BattleContext?.TargetRegistry;
            if (registry == null)
                return 0;

            IReadOnlyList<ICombatUnit> targets = registry.HostileTargets;
            float sqrRange = range * range;
            int count = 0;

            for (int i = targets.Count - 1; i >= 0; i--)
            {
                ICombatUnit target = targets[i];
                if (!HasValidTarget(target))
                    continue;

                Vector3 position = target.Position;
                position.y = center.y;

                if ((position - center).sqrMagnitude <= sqrRange)
                    count++;
            }

            return count;
        }

        private void ResolveEnemiesInArea(
            SkillRuntimeContext context,
            Vector3 areaOrigin,
            SkillAreaData areaData)
        {
            IBattleTargetRegistry registry = context?.BattleContext?.TargetRegistry;
            if (registry == null || areaData == null)
                return;

            Vector3 castDirection = ResolveCastDirection(context);
            Vector3 areaCenter = areaData.ResolveAreaCenter(areaOrigin, castDirection);
            IReadOnlyList<ICombatUnit> targets = registry.HostileTargets;

            for (int i = targets.Count - 1; i >= 0; i--)
            {
                ICombatUnit target = targets[i];
                if (IsUnitInsideArea(target, areaData, areaCenter, castDirection))
                    buffer.Add(target);
            }
        }


        private void ResolveAllAllies()
        {
            UserDataCache cache = UserDataCache.Instance;
            if (cache == null || cache.inBattleHeroes == null)
                return;

            for (int i = 0; i < cache.inBattleHeroes.Length; i++)
                AddIfValid(cache.inBattleHeroes[i]);
        }

        private void ResolveAllEnemies(SkillRuntimeContext context)
        {
            IBattleTargetRegistry registry = context?.BattleContext?.TargetRegistry;
            if (registry == null)
                return;

            IReadOnlyList<ICombatUnit> targets = registry.HostileTargets;
            for (int i = targets.Count - 1; i >= 0; i--)
                AddIfValid(targets[i]);
        }

        private bool IsUnitInsideArea(
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
                    return IsInsideBox(
                        unitPosition,
                        areaCenter,
                        castDirection,
                        areaData.BoxLength,
                        areaData.BoxWidth);

                case SkillAreaShape.Circle:
                default:
                    float radius = Mathf.Max(0f, areaData.Radius);
                    return (unitPosition - areaCenter).sqrMagnitude <= radius * radius;
            }
        }

        public bool HasValidTarget(ICombatUnit target)
        {
            return target != null && target.IsUnityAlive() && !target.IsDead;
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

            return Mathf.Abs(forwardDistance) <= halfLength
                   && Mathf.Abs(sideDistance) <= halfWidth;
        }

        private static Vector3 ResolveCastDirection(SkillRuntimeContext context)
        {
            if (context == null)
                return Vector3.right;

            Vector3 origin = context.Caster != null
                ? context.Caster.Position
                : context.CastPosition;

            Vector3 destination;
            if (context.MainTarget != null &&
                context.MainTarget.IsUnityAlive() &&
                !context.MainTarget.IsDead)
            {
                destination = context.MainTarget.Position;
            }
            else if ((context.TargetPosition - origin).sqrMagnitude > 0.0001f)
            {
                destination = context.TargetPosition;
            }
            else
            {
                destination = context.CastPosition;
            }

            Vector3 direction = destination - origin;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f && context.Caster != null)
            {
                direction = context.Caster.transform.forward;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector3.right;

            return direction.normalized;
        }

        private void AddIfValid(ICombatUnit unit)
        {
            if (HasValidTarget(unit) && !buffer.Contains(unit))
                buffer.Add(unit);
        }
    }
}
