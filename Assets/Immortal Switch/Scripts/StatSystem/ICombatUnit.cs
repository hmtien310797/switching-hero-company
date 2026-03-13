namespace Immortal_Switch.Scripts.StatSystem
{
    public interface ICombatUnit
    {
        StatsController Stats { get; }
        bool IsDead { get; }
        float CurrentHp { get; }
        float MaxHp { get; }

        void TakeDamage(float amount, DamageType damageType = DamageType.Normal);
        void Heal(float amount);
    }
}