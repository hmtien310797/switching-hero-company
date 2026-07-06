using System.Collections.Generic;
using Immortal_Switch.Scripts.PowerUpSystem;

namespace Immortal_Switch.Scripts.StatSystem
{
    public class BuffInstance
    {
        public BuffData Data { get; private set; }
        public float RemainingTime { get; private set; }
        public int StackCount { get; private set; }

        private float tickTimer;
        private readonly List<StatModifier> appliedModifiers = new();

        public string RuntimeSourceId => StatSourceIds.Buff(Data.Id);

        public BuffInstance(BuffData data)
        {
            Data = data;
            RemainingTime = data.Duration;
            StackCount = 1;
            tickTimer = data.TickInterval;
        }

        public void RefreshDuration()
        {
            RemainingTime = Data.Duration;
        }

        public void Tick(float deltaTime)
        {
            RemainingTime -= deltaTime;

            if (Data.PeriodicEffectType != PeriodicEffectType.None && Data.TickInterval > 0f)
            {
                tickTimer -= deltaTime;
            }
        }

        public bool CanTriggerPeriodic()
        {
            return Data.PeriodicEffectType != PeriodicEffectType.None
                   && Data.TickInterval > 0f
                   && tickTimer <= 0f;
        }

        public void ResetTickTimer()
        {
            tickTimer += Data.TickInterval;
        }

        public bool IsExpired => RemainingTime <= 0f;

        public bool CanStack()
        {
            return StackCount < Data.MaxStacks;
        }

        public void AddStack()
        {
            StackCount++;
        }

        public IReadOnlyList<StatModifier> GetAppliedModifiers()
        {
            return appliedModifiers;
        }

        public void AddAppliedModifier(StatModifier modifier)
        {
            appliedModifiers.Add(modifier);
        }

        public void ClearAppliedModifiers()
        {
            appliedModifiers.Clear();
        }

        public void ReplaceData(BuffData newData)
        {
            Data = newData;
            RemainingTime = newData.Duration;
            StackCount = 1;
            tickTimer = newData.TickInterval;
            appliedModifiers.Clear();
        }
    }
}