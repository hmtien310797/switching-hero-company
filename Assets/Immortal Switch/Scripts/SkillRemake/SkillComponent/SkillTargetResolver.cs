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

        public List<ICombatUnit> ResolveTargets(SkillRuntimeContext context, SkillTargetType targetType, SkillAreaData areaData = null)
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
                    AddIfValid(context.BattleController != null && context.Caster != null
                        ? context.BattleController.GetNearestEnemy(context.Caster.Position)
                        : null);
                    break;

                case SkillTargetType.AreaAroundSelf:
                    ResolveEnemiesInArea(context, context.Caster != null ? context.Caster.Position : context.CastPosition, areaData);
                    break;

                case SkillTargetType.AreaAroundTarget:
                    ResolveEnemiesInArea(context, context.MainTarget != null ? context.MainTarget.Position : context.TargetPosition, areaData);
                    break;

                case SkillTargetType.AreaAroundCastPosition:
                    ResolveEnemiesInArea(context, context.CastPosition, areaData);
                    break;

                case SkillTargetType.AllEnemies:
                    ResolveEnemiesInArea(context, context.Caster != null ? context.Caster.Position : context.CastPosition, null, float.MaxValue);
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
                return 0;

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

        private void ResolveEnemiesInArea(SkillRuntimeContext context, Vector3 center, SkillAreaData areaData, float overrideRadius = -1f)
        {
            if (context.BattleController == null)
                return;
            
            BossActor boss = context.BattleController.GetActiveBossActor();
            if(boss != null)
                buffer.Add(boss);

            float radius = overrideRadius > 0f ? overrideRadius : areaData != null ? areaData.Radius : 0f;
            float sqrRadius = radius * radius;
            List<EnemyActor> enemies = context.BattleController.CreepList;

            if (enemies == null)
                return;

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyActor enemy = enemies[i];
                if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
                    continue;

                Vector3 pos = enemy.Position;
                pos.y = center.y;

                if ((pos - center).sqrMagnitude <= sqrRadius)
                    buffer.Add(enemy);
            }
        }

        private void AddIfValid(ICombatUnit unit)
        {
            if (unit != null && !unit.IsDead)
                buffer.Add(unit);
        }
    }
}
