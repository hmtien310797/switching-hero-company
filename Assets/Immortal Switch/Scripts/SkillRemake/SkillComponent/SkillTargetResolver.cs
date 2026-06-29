using System.Collections.Generic;
using Immortal_Switch.Scripts.Enemy;
using UnityEngine;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Skill
{
    public sealed class SkillTargetResolver
    {
        private readonly List<ICombatUnit> buffer = new();

        public ICombatUnit ResolveMainTarget(SkillRuntimeContext context, SkillTargetSelectType selectType)
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
                    return context.BattleController != null && context.Caster != null
                        ? context.BattleController.GetNearestEnemy(context.Caster.Position)
                        : null;

                case SkillTargetSelectType.NearestEnemy:
                default:
                    return context.BattleController != null && context.Caster != null
                        ? context.BattleController.GetNearestEnemy(context.Caster.Position)
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
                        context.BattleController != null &&
                        context.Caster != null
                            ? context.BattleController
                                .GetNearestEnemy(
                                    context.Caster.Position)
                            : null);
                    break;

                case SkillTargetType.AreaAroundSelf:
                {
                    Vector3 origin =
                        context.Caster != null
                            ? context.Caster.Position
                            : context.CastPosition;

                    ResolveEnemiesInArea(
                        context,
                        origin,
                        areaData);

                    break;
                }

                case SkillTargetType.AreaAroundTarget:
                {
                    Vector3 origin =
                        context.MainTarget != null
                            ? context.MainTarget.Position
                            : context.TargetPosition;

                    ResolveEnemiesInArea(
                        context,
                        origin,
                        areaData);

                    break;
                }

                case SkillTargetType.AreaAroundCastPosition:
                    ResolveEnemiesInArea(
                        context,
                        context.CastPosition,
                        areaData);
                    break;

                case SkillTargetType.AllEnemies:
                    ResolveAllEnemies(context);
                    break;
            }

            return buffer;
        }

        public int CountEnemiesInRange(SkillRuntimeContext context, Vector3 center, float range)
        {
            if (context == null || context.BattleController == null)
                return 0;

            int count = 0;
            float sqrRange = range * range;
            
            BossActor boss = context.BattleController.GetActiveBossActor();
            if (boss != null)
            {
                Vector3 pos = boss.Position;
                pos.y = center.y;

                if ((pos - center).sqrMagnitude <= sqrRange)
                    count++;
            }
            
            List<EnemyActor> enemies = context.BattleController.CreepList;

            if (enemies == null)
                return count;

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyActor enemy = enemies[i];
                if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
                    continue;

                Vector3 pos = enemy.Position;
                pos.y = center.y;

                if ((pos - center).sqrMagnitude <= sqrRange)
                    count++;
            }

            return count;
        }

        private void ResolveEnemiesInArea(
            SkillRuntimeContext context,
            Vector3 areaOrigin,
            SkillAreaData areaData)
        {
            if (context == null ||
                context.BattleController == null)
            {
                return;
            }

            if (areaData == null)
                return;

            Vector3 castDirection =
                ResolveCastDirection(context);

            Vector3 areaCenter =
                areaData.ResolveAreaCenter(
                    areaOrigin,
                    castDirection);

            BossActor boss =
                context.BattleController
                    .GetActiveBossActor();

            if (IsUnitInsideArea(
                    boss,
                    areaData,
                    areaCenter,
                    castDirection))
            {
                buffer.Add(boss);
            }

            List<EnemyActor> enemies =
                context.BattleController.CreepList;

            if (enemies == null)
                return;

            for (int i = enemies.Count - 1;
                 i >= 0;
                 i--)
            {
                EnemyActor enemy = enemies[i];

                if (!IsUnitInsideArea(
                        enemy,
                        areaData,
                        areaCenter,
                        castDirection))
                {
                    continue;
                }

                buffer.Add(enemy);
            }
        }
        
        private void ResolveAllEnemies(
            SkillRuntimeContext context)
        {
            if (context == null ||
                context.BattleController == null)
            {
                return;
            }

            BossActor boss =
                context.BattleController
                    .GetActiveBossActor();

            AddIfValid(boss);

            List<EnemyActor> enemies =
                context.BattleController.CreepList;

            if (enemies == null)
                return;

            for (int i = enemies.Count - 1;
                 i >= 0;
                 i--)
            {
                EnemyActor enemy = enemies[i];

                if (enemy == null ||
                    enemy.IsDead ||
                    !enemy.gameObject.activeInHierarchy)
                {
                    continue;
                }

                buffer.Add(enemy);
            }
        }
        
        private bool IsUnitInsideArea(
            ICombatUnit unit,
            SkillAreaData areaData,
            Vector3 areaCenter,
            Vector3 castDirection)
        {
            if (!HasValidTarget(unit) || areaData == null)
            {
                return false;
            }

            Vector3 unitPosition =
                unit.Position;

            unitPosition.y =
                areaCenter.y;

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
                {
                    float radius =
                        Mathf.Max(0f, areaData.Radius);

                    float sqrRadius =
                        radius * radius;

                    return (unitPosition - areaCenter)
                        .sqrMagnitude <= sqrRadius;
                }
            }
        }
        
        public bool HasValidTarget(ICombatUnit currentTarget)
        {
            if (!currentTarget.IsUnityAlive())
            {
                return false;
            }
            return !currentTarget.IsDead;
        }
        
        private bool IsInsideBox(
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

            Vector3 rightDirection =
                new Vector3(
                    -forwardDirection.z,
                    0f,
                    forwardDirection.x);

            Vector3 offset =
                targetPosition - boxCenter;

            offset.y = 0f;

            float forwardDistance =
                Vector3.Dot(
                    offset,
                    forwardDirection);

            float sideDistance =
                Vector3.Dot(
                    offset,
                    rightDirection);

            float halfLength =
                Mathf.Max(0f, boxLength) * 0.5f;

            float halfWidth =
                Mathf.Max(0f, boxWidth) * 0.5f;

            return Mathf.Abs(forwardDistance)
                   <= halfLength
                   && Mathf.Abs(sideDistance)
                   <= halfWidth;
        }
        
        private Vector3 ResolveCastDirection(
            SkillRuntimeContext context)
        {
            if (context == null)
                return Vector3.right;
            
            if (!context.MainTarget.IsUnityAlive())
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

        private void AddIfValid(ICombatUnit unit)
        {
            if (unit != null && !unit.IsDead)
                buffer.Add(unit);
        }
    }
}
