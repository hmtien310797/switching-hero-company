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

        void TakeDamage(ICombatUnit attacker, float amount = 1);

        void Heal(float amount);
    }
}