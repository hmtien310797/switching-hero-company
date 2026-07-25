using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.DamageNumber;
using UnityEngine;

namespace Immortal_Switch.Scripts.StatSystem
{
    public interface ICombatUnit
    {
        ActorType ActorType { get; }
        StatsController Stats { get; }
        Element Element { get; }
        HealthBarController HealthBarController { get; }
        Transform Transform { get; }
        Vector3 Position { get; }
        bool IsDead { get; }
        float CurrentHp { get; }
        float MaxHp { get; }
        DamageResult TakeDamage(DamageResult damageResult)
        {
            float appliedDamage =
                Stats.HealthModule.TakeDamage(damageResult);

            if (appliedDamage <= 0f)
            {
                return damageResult;
            }

            HealthBarController?.SetHealth(CurrentHp, MaxHp);

            DamageNumberService.Instance?.ShowDamage(
                appliedDamage,
                Position,
                damageResult.DamageType
            );

            return damageResult;
        }

        void Heal(float amount);
    }

    public enum ActorType
    {
        Hero,
        Boss,
        Creep,
        EnemyHero,
        DamageDummy,
        DefenseObjective
    }
}