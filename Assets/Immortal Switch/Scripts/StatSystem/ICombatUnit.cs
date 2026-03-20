namespace Immortal_Switch.Scripts.StatSystem
{
    public interface ICombatUnit
    {
        StatsController Stats { get; }
        bool IsDead { get; }
        float CurrentHp { get; }
        float MaxHp { get; }
        void TakeDamage(ICombatUnit attacker, float amount = 1);
        void Heal(float amount);
    }
    
}