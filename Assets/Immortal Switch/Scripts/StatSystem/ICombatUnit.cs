using Immortal_Switch.Scripts.Combat;
using UnityEngine;

namespace Immortal_Switch.Scripts.StatSystem
{
    public interface ICombatUnit
    {
        StatsController Stats { get; }

        Transform Transform { get; }

        Vector3 Position { get; }

        bool IsDead { get; }

        float CurrentHp { get; }

        float MaxHp { get; }

        void TakeDamage(ICombatUnit attacker, float amount = 1)
        {
            DamageResult damageResult = DamageCalculator.CalculateDamage(attacker, (ICombatUnit)this, amount);
            Stats.HealthModule.TakeDamage(damageResult.Damage, damageResult.DamageTextType);
            //healthBarController?.SetHealth(CurrentHp / MaxHp);
            // if(dameTrans != null)
            //     healthBarController?.ShowHealthTxt((int)damageResult.Damage, dameTrans.position);
        }

        void Heal(float amount);
    }
}