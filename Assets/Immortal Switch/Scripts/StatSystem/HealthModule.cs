using System;
using UnityEngine;

namespace Immortal_Switch.Scripts.StatSystem
{
    [Serializable]
    public class HealthModule
    {
        public event Action<float, float> OnHPChanged;
        public event Action<float, DamageType> OnDamaged;
        public event Action<float> OnHealed;
        public event Action OnDead;

        private readonly StatModule statModule;

        [field: SerializeField]
        public float CurrentHP { get; private set; }
        public float MaxHP => statModule.GetFinalStat(StatType.MaxHP);
        public bool IsDead => CurrentHP <= 0f;

        public HealthModule(StatModule statModule)
        {
            this.statModule = statModule;
            this.statModule.OnStatChanged += OnStatChanged;
        }

        public void Init()
        {
            CurrentHP = MaxHP;
            OnHPChanged?.Invoke(CurrentHP, MaxHP);
        }

        public void SetFull()
        {
            CurrentHP = MaxHP;
            OnHPChanged?.Invoke(CurrentHP, MaxHP);
        }

        public void TakeDamage(float amount, DamageType damageType = DamageType.Normal)
        {
            if (amount <= 0f || IsDead)
                return;

            float finalDamage = amount;

            if (damageType != DamageType.True)
            {
                float reduction = statModule.GetFinalStat(StatType.DamageReduction);
                finalDamage *= (1f - reduction);
            }

            CurrentHP -= finalDamage;
            if (CurrentHP < 0f) CurrentHP = 0f;

            OnDamaged?.Invoke(finalDamage, damageType);
            OnHPChanged?.Invoke(CurrentHP, MaxHP);

            if (CurrentHP <= 0f)
                OnDead?.Invoke();
        }

        public void ApplyHeal(float amount)
        {
            if (amount <= 0f || IsDead)
                return;

            CurrentHP += amount;
            if (CurrentHP > MaxHP) CurrentHP = MaxHP;

            OnHealed?.Invoke(amount);
            OnHPChanged?.Invoke(CurrentHP, MaxHP);
        }

        private void OnStatChanged(StatType statType, float oldValue, float newValue)
        {
            if (statType != StatType.MaxHP)
                return;

            float ratio = oldValue > 0f ? CurrentHP / oldValue : 1f;
            CurrentHP = newValue * ratio;

            if (CurrentHP < 0f) CurrentHP = 0f;
            if (CurrentHP > MaxHP) CurrentHP = MaxHP;

            OnHPChanged?.Invoke(CurrentHP, MaxHP);
        }

        public void Dispose()
        {
            if (statModule != null)
            {
                statModule.OnStatChanged -= OnStatChanged;
            }
        }
    }
}