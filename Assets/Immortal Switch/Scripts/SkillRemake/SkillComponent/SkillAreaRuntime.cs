using System.Collections.Generic;
using Common;
using UnityEngine;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Skill
{
    public sealed class SkillAreaRuntime : PoolableBehaviour
    {
        private readonly HashSet<ICombatUnit> hitTargets = new();

        private SkillRuntimeContext context;
        private SkillAreaData data;
        private SkillExecutor executor;
        private SkillTargetResolver targetResolver;
        private ISkillObjectSpawner spawner;
        private float lifeTimer;
        private float tickTimer;
        private bool initialized;

        public void Init(
            SkillRuntimeContext context,
            SkillAreaData data,
            SkillExecutor executor,
            SkillTargetResolver targetResolver,
            ISkillObjectSpawner spawner)
        {
            this.context = context;
            this.data = data;
            this.executor = executor;
            this.targetResolver = targetResolver;
            this.spawner = spawner;

            lifeTimer = data.Duration;
            tickTimer = 0f;
            hitTargets.Clear();
            initialized = true;

            if (data.Duration <= 0f)
            {
                Tick();
                DespawnSelf();
            }
        }

        private void Update()
        {
            if (!initialized || data == null)
                return;

            if (data.Duration <= 0f)
                return;

            lifeTimer -= Time.deltaTime;
            tickTimer -= Time.deltaTime;

            if (tickTimer <= 0f)
            {
                Tick();
                tickTimer = Mathf.Max(0.01f, data.TickInterval);
            }

            if (lifeTimer <= 0f)
                DespawnSelf();
        }

        private void Tick()
        {
            if (context == null || data == null || executor == null || targetResolver == null)
                return;

            SkillRuntimeContext areaContext = new SkillRuntimeContext
            {
                Caster = context.Caster,
                MainTarget = context.MainTarget,
                SkillData = context.SkillData,
                SkillLevel = context.SkillLevel,
                CastPosition = context.CastPosition,
                TargetPosition = transform.position,
                BattleController = context.BattleController,
                SkillController = context.SkillController,
                RuntimeObject = context.RuntimeObject
            };

            List<ICombatUnit> targets = targetResolver.ResolveTargets(areaContext, SkillTargetType.AreaAroundTarget, data);
            for (int i = 0; i < targets.Count; i++)
            {
                ICombatUnit target = targets[i];
                if (target == null || target.IsDead)
                    continue;

                if (data.HitOncePerTarget && hitTargets.Contains(target))
                    continue;

                hitTargets.Add(target);
                executor.ExecuteNestedActions(areaContext.CloneForTarget(target), data.OnHitActions);
            }
        }

        public override void OnDespawnedToPool()
        {
            initialized = false;
            context = null;
            data = null;
            executor = null;
            targetResolver = null;
            spawner = null;
            hitTargets.Clear();
        }
    }
}
