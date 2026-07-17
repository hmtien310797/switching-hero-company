using System.Collections.Generic;
using Common;
using UnityEngine;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Skill
{
    public sealed class SkillAreaRuntime : MonoBehaviour
    {
        private readonly HashSet<ICombatUnit> hitTargets = new();
        private readonly Dictionary<ICombatUnit, List<string>> activeAreaBuffIds = new();
        private readonly List<ICombatUnit> areaBuffTargetBuffer = new();
        private readonly List<ICombatUnit> areaBuffRemoveUnitBuffer = new();
        private readonly List<string> areaBuffRemoveIdBuffer = new();
        private readonly HashSet<string> areaBuffActiveThisTick = new();
        private readonly List<ICombatUnit> tickTargetBuffer = new();

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
            ClearAllAreaBuffs();
            initialized = true;

            if (data.Duration <= 0f)
            {
                Tick();
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

            // if (lifeTimer <= 0f)
            //     DespawnSelf();
        }

        private void Tick()
        {
            if (context == null || data == null || executor == null || targetResolver == null)
                return;

            SkillRuntimeContext areaContext = new SkillRuntimeContext
            {
                Caster = context.Caster,
                // Area runtime phải dùng chính vị trí area hiện tại,
                // không bám theo MainTarget gốc sau khi đã spawn.
                MainTarget = null,
                SkillData = context.SkillData,
                SkillLevel = context.SkillLevel,
                CastPosition = context.CastPosition,
                TargetPosition = transform.position,
                BattleContext = context.BattleContext,
                SkillController = context.SkillController,
                RuntimeObject = context.RuntimeObject
            };

            List<ICombatUnit> targets = targetResolver.ResolveTargets(areaContext, SkillTargetType.AreaAroundTarget, data);
            tickTargetBuffer.Clear();
            tickTargetBuffer.AddRange(targets);

            RefreshAreaBuffs(areaContext);

            for (int i = 0; i < tickTargetBuffer.Count; i++)
            {
                ICombatUnit target = tickTargetBuffer[i];
                if (target == null || target.IsDead)
                    continue;

                if (data.HitOncePerTarget && hitTargets.Contains(target))
                    continue;

                hitTargets.Add(target);
                executor.ExecuteNestedActions(areaContext.CloneForTarget(target), data.OnHitActions);
            }
        }


        private void RefreshAreaBuffs(SkillRuntimeContext areaContext)
        {
            if (data == null || data.OnHitActions == null || executor == null)
                return;

            areaBuffActiveThisTick.Clear();

            for (int actionIndex = 0; actionIndex < data.OnHitActions.Count; actionIndex++)
            {
                SkillActionData action = data.OnHitActions[actionIndex];
                if (!executor.IsWhileInAreaBuffAction(action))
                    continue;

                executor.ResolveBuffTargets(
                    areaContext,
                    action,
                    data,
                    areaBuffTargetBuffer,
                    onlyTargetsInsideArea: true);

                BuffKind kind = action.ActionType == SkillActionType.ApplyDebuff
                    ? BuffKind.Debuff
                    : BuffKind.Buff;

                string idSuffix = $"area_{GetInstanceID()}_{actionIndex}";

                for (int i = 0; i < areaBuffTargetBuffer.Count; i++)
                {
                    ICombatUnit target = areaBuffTargetBuffer[i];
                    if (target == null || target.IsDead || target.Stats == null || target.Stats.BuffModule == null)
                        continue;

                    BuffData buffData = executor.CreateBuffData(areaContext, action, kind, idSuffix);
                    if (buffData == null)
                        continue;

                    string pairKey = GetAreaBuffPairKey(target, buffData.Id);
                    areaBuffActiveThisTick.Add(pairKey);

                    if (HasAreaBuff(target, buffData.Id))
                        continue;

                    // WhileInArea sống theo area, không theo duration riêng.
                    // Remove thủ công khi target rời vùng hoặc area despawn.
                    buffData.Duration = 999999f;

                    target.Stats.BuffModule.ApplyBuff(buffData);
                    RegisterAreaBuff(target, buffData.Id);
                }

                areaBuffTargetBuffer.Clear();
            }

            RemoveAreaBuffsNotInThisTick();
        }

        private void RegisterAreaBuff(ICombatUnit target, string buffId)
        {
            if (target == null || string.IsNullOrEmpty(buffId))
                return;

            if (!activeAreaBuffIds.TryGetValue(target, out List<string> buffIds))
            {
                buffIds = new List<string>();
                activeAreaBuffIds.Add(target, buffIds);
            }

            if (!buffIds.Contains(buffId))
                buffIds.Add(buffId);
        }

        private bool HasAreaBuff(ICombatUnit target, string buffId)
        {
            if (target == null || string.IsNullOrEmpty(buffId))
                return false;

            return activeAreaBuffIds.TryGetValue(target, out List<string> buffIds) &&
                   buffIds.Contains(buffId);
        }

        private string GetAreaBuffPairKey(ICombatUnit target, string buffId)
        {
            int targetId = target is Component component
                ? component.GetInstanceID()
                : target.GetHashCode();

            return $"{targetId}:{buffId}";
        }

        private void RemoveAreaBuffsNotInThisTick()
        {
            areaBuffRemoveUnitBuffer.Clear();
            areaBuffRemoveIdBuffer.Clear();

            foreach (KeyValuePair<ICombatUnit, List<string>> pair in activeAreaBuffIds)
            {
                ICombatUnit target = pair.Key;
                List<string> buffIds = pair.Value;

                for (int i = 0; i < buffIds.Count; i++)
                {
                    string buffId = buffIds[i];
                    string pairKey = GetAreaBuffPairKey(target, buffId);

                    if (areaBuffActiveThisTick.Contains(pairKey))
                        continue;

                    areaBuffRemoveUnitBuffer.Add(target);
                    areaBuffRemoveIdBuffer.Add(buffId);
                }
            }

            for (int i = 0; i < areaBuffRemoveUnitBuffer.Count; i++)
                RemoveAreaBuff(areaBuffRemoveUnitBuffer[i], areaBuffRemoveIdBuffer[i]);

            areaBuffRemoveUnitBuffer.Clear();
            areaBuffRemoveIdBuffer.Clear();
        }

        private void RemoveAreaBuff(ICombatUnit target, string buffId)
        {
            if (target != null && !string.IsNullOrEmpty(buffId))
            {
                if (target.IsUnityAlive() && target.Stats != null && target.Stats.BuffModule != null)
                    target.Stats.BuffModule.RemoveBuffById(buffId);
            }

            if (target == null || !activeAreaBuffIds.TryGetValue(target, out List<string> buffIds))
                return;

            buffIds.Remove(buffId);
            if (buffIds.Count == 0)
                activeAreaBuffIds.Remove(target);
        }

        private void ClearAllAreaBuffs()
        {
            areaBuffRemoveUnitBuffer.Clear();
            areaBuffRemoveIdBuffer.Clear();

            foreach (KeyValuePair<ICombatUnit, List<string>> pair in activeAreaBuffIds)
            {
                ICombatUnit target = pair.Key;
                List<string> buffIds = pair.Value;

                for (int i = 0; i < buffIds.Count; i++)
                {
                    areaBuffRemoveUnitBuffer.Add(target);
                    areaBuffRemoveIdBuffer.Add(buffIds[i]);
                }
            }

            for (int i = 0; i < areaBuffRemoveUnitBuffer.Count; i++)
                RemoveAreaBuff(areaBuffRemoveUnitBuffer[i], areaBuffRemoveIdBuffer[i]);

            activeAreaBuffIds.Clear();
            areaBuffRemoveUnitBuffer.Clear();
            areaBuffRemoveIdBuffer.Clear();
            areaBuffActiveThisTick.Clear();
            areaBuffTargetBuffer.Clear();
            tickTargetBuffer.Clear();
        }

        public void OnDisable()
        {
            ClearAllAreaBuffs();
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
