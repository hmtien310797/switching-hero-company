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

        DamageResult TakeDamage(ICombatUnit attacker, DamageResult damageResult)
        {
            //DamageResult damageResult = DamageCalculator.CalculateDamage(attacker, this, amount);
            Stats.HealthModule.TakeDamage(damageResult);
            //healthBarController?.SetHealth(CurrentHp / MaxHp);
            // if(dameTrans != null)
            //     healthBarController?.ShowHealthTxt((int)damageResult.Damage, dameTrans.position);
            Debug.Log($"<color=green>{attacker.Stats.name}</color> ----> <color=red>{Stats.name}</color> {damageResult.Damage}");
            return damageResult;
        }

        void Heal(float amount);
    }
}