using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.DamageNumber;
using UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.StatSystem
{
    public interface ICombatUnit
    {
        StatsController Stats { get; }
        HealthBarController HealthBarController { get; }

        Transform Transform { get; }

        Vector3 Position { get; }

        bool IsDead { get; }

        float CurrentHp { get; }

        float MaxHp { get; }

        DamageResult TakeDamage(ICombatUnit attacker, DamageResult damageResult)
        {
            Stats.HealthModule.TakeDamage(damageResult);
            HealthBarController?.SetHealth(CurrentHp / MaxHp);
            DamageNumberService.Instance?.ShowDamage(
                damageResult.Damage,
                Position,
                damageResult.DamageType
            );
            Debug.Log($"<color=green>{attacker.Stats.name}</color> ----> <color=red>{Stats.name}</color> {damageResult.Damage}");
            return damageResult;
        }

        void Heal(float amount);
    }
}