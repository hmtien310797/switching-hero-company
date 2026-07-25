using System;
using Immortal_Switch.Scripts.Combat;
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

        // Có thể dùng để hiện chữ Immune hoặc hiệu ứng khi bị đánh.
        public event Action<DamageResult> OnDamageBlocked;

        private readonly StatModule statModule;
        private readonly StatusEffectModule statusEffectModule;

        [field: SerializeField]
        public float CurrentHP { get; private set; }

        public float MaxHP => statModule.GetFinalStat(StatType.MaxHp);
        public bool IsDead => CurrentHP <= 0f;

        public bool IsInvincible =>
            statusEffectModule != null &&
            statusEffectModule.HasStatus(StatusEffectType.Invincible);

        public HealthModule(
            StatModule statModule,
            StatusEffectModule statusEffectModule)
        {
            this.statModule = statModule;
            this.statusEffectModule = statusEffectModule;

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

        /// <summary>
        /// Trả về lượng damage thực tế đã trừ vào HP.
        /// Trả về 0 nếu damage bị chặn.
        /// </summary>
        public float TakeDamage(DamageResult damageResult)
        {
            if (damageResult.Damage <= 0f ||
                IsDead)
            {
                return 0f;
            }

            // Bất tử chặn toàn bộ damage, kể cả True Damage và DOT.
            if (IsInvincible)
            {
                OnDamageBlocked?.Invoke(damageResult);
                return 0f;
            }

            float finalDamage = damageResult.Damage;

            if (damageResult.DamageType != DamageType.True)
            {
                float reduction =
                    statModule.GetFinalStat(StatType.DamageReduction);

                finalDamage *= 1f - reduction;
            }

            finalDamage = Mathf.Max(0f, finalDamage);

            if (finalDamage <= 0f)
                return 0f;

            CurrentHP -= finalDamage;
            CurrentHP = Mathf.Max(0f, CurrentHP);

            OnDamaged?.Invoke(finalDamage, damageResult.DamageType);
            OnHPChanged?.Invoke(CurrentHP, MaxHP);

            if (CurrentHP <= 0f)
            {
                OnDead?.Invoke();
            }

            return finalDamage;
        }

        public void ApplyHeal(float amount)
        {
            if (amount <= 0f || IsDead)
                return;

            float oldHP = CurrentHP;

            CurrentHP += amount;
            CurrentHP = Mathf.Min(CurrentHP, MaxHP);

            float actualHeal = CurrentHP - oldHP;

            if (actualHeal <= 0f)
                return;

            OnHealed?.Invoke(actualHeal);
            OnHPChanged?.Invoke(CurrentHP, MaxHP);
        }

        private void OnStatChanged(
            StatType statType,
            float oldValue,
            float newValue)
        {
            if (statType != StatType.MaxHp)
                return;

            float ratio = oldValue > 0f
                ? CurrentHP / oldValue
                : 1f;

            CurrentHP = newValue * ratio;
            CurrentHP = Mathf.Clamp(CurrentHP, 0f, MaxHP);

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