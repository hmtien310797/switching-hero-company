using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Combat;

namespace Immortal_Switch.Scripts.StatSystem
{
    [Serializable]
    public class BuffModule
    {
        public event Action<BuffInstance> OnBuffApplied;
        public event Action<BuffInstance> OnBuffRemoved;
        public event Action<BuffInstance> OnBuffRefreshed;
        public event Action<BuffInstance> OnBuffStackChanged;
        public event Action<BuffInstance> OnBuffTick;

        private readonly StatModule statModule;
        private readonly HealthModule healthModule;
        private readonly StatusEffectModule statusEffectModule;

        private readonly List<BuffInstance> activeBuffs = new();

        public BuffModule(
            StatModule statModule,
            HealthModule healthModule,
            StatusEffectModule statusEffectModule)
        {
            this.statModule = statModule;
            this.healthModule = healthModule;
            this.statusEffectModule = statusEffectModule;
        }

        public IReadOnlyList<BuffInstance> ActiveBuffs => activeBuffs;

        public void Update(float deltaTime)
        {
            if (activeBuffs == null || activeBuffs.Count == 0)
            {
                return;
            }
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = activeBuffs[i];  
                buff.Tick(deltaTime);

                while (buff.CanTriggerPeriodic())
                {
                    TriggerPeriodic(buff);
                    buff.ResetTickTimer();
                }

                if (buff.IsExpired)
                {
                    RemoveBuff(buff);
                }
            }
        }

        public void ApplyBuff(BuffData data)
        {
            var existing = FindBuffById(data.Id);

            if (existing == null)
            {
                var instance = new BuffInstance(data);
                activeBuffs.Add(instance);

                ApplyInstanceModifiers(instance, true);
                ApplyStatus(instance);

                OnBuffApplied?.Invoke(instance);
                return;
            }

            HandleExistingBuff(existing, data);
        }

        public bool RemoveBuffById(string buffId)
        {
            var buff = FindBuffById(buffId);
            if (buff == null) return false;

            RemoveBuff(buff);
            return true;
        }

        public bool HasBuff(string buffId)
        {
            return FindBuffById(buffId) != null;
        }

        public BuffInstance FindBuffById(string buffId)
        {
            for (int i = 0; i < activeBuffs.Count; i++)
            {
                if (activeBuffs[i].Data.Id == buffId)
                    return activeBuffs[i];
            }

            return null;
        }

        private void HandleExistingBuff(BuffInstance existing, BuffData newData)
        {
            switch (newData.StackRule)
            {
                case BuffStackRule.None:
                    break;

                case BuffStackRule.Refresh:
                    existing.RefreshDuration();
                    OnBuffRefreshed?.Invoke(existing);
                    break;

                case BuffStackRule.Replace:
                    RemoveInstanceModifiers(existing);
                    RemoveStatus(existing);

                    existing.ReplaceData(newData);

                    ApplyInstanceModifiers(existing, true);
                    ApplyStatus(existing);

                    OnBuffRefreshed?.Invoke(existing);
                    break;

                case BuffStackRule.Stack:
                    if (existing.CanStack())
                    {
                        existing.AddStack();
                        ApplyInstanceModifiers(existing, true);
                        OnBuffStackChanged?.Invoke(existing);
                    }

                    existing.RefreshDuration();
                    OnBuffRefreshed?.Invoke(existing);
                    break;
            }
        }

        private void ApplyInstanceModifiers(BuffInstance instance, bool applySingleStack)
        {
            int count = applySingleStack ? 1 : instance.StackCount;

            for (int s = 0; s < count; s++)
            {
                for (int i = 0; i < instance.Data.Modifiers.Count; i++)
                {
                    var cloned = instance.Data.Modifiers[i].Clone();
                    cloned.SourceId = instance.RuntimeSourceId;
                    statModule.AddModifier(cloned);
                    instance.AddAppliedModifier(cloned);
                }
            }
        }

        private void RemoveInstanceModifiers(BuffInstance instance)
        {
            var mods = instance.GetAppliedModifiers();

            for (int i = 0; i < mods.Count; i++)
            {
                statModule.RemoveModifier((StatModifier)mods[i]);
            }

            instance.ClearAppliedModifiers();
        }

        private void ApplyStatus(BuffInstance instance)
        {
            if (instance.Data.StatusEffects != StatusEffectType.None)
            {
                statusEffectModule.AddStatus(instance.Data.StatusEffects);
            }
        }

        private void RemoveStatus(BuffInstance instance)
        {
            if (instance.Data.StatusEffects != StatusEffectType.None)
            {
                statusEffectModule.RemoveStatus(instance.Data.StatusEffects);
            }
        }

        private void TriggerPeriodic(BuffInstance instance)
        {
            float value = instance.Data.PeriodicValue * instance.StackCount;

            switch (instance.Data.PeriodicEffectType)
            {
                case PeriodicEffectType.DamageOverTime:
                    DamageResult damageResult = new DamageResult
                    {
                        Damage = value,
                        DamageType = instance.Data.PeriodicDamageType,
                    };
                    healthModule.TakeDamage(damageResult);
                    break;

                case PeriodicEffectType.HealOverTime:
                    healthModule.ApplyHeal(value);
                    break;
            }

            OnBuffTick?.Invoke(instance);
        }

        private void RemoveBuff(BuffInstance instance)
        {
            RemoveInstanceModifiers(instance);
            RemoveStatus(instance);
            activeBuffs.Remove(instance);
            OnBuffRemoved?.Invoke(instance);
        }
    }
}