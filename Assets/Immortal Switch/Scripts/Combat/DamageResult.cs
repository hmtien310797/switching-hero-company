using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Combat
{
    public struct DamageResult
    {
        public float Damage;
        public DamageType DamageType;

        public float BaseATK;
        public float SkillCoefficient;
        public float DefenseMultiplier;

        public float FlatATKBonus;
        public float ATKPercentBonus;
    }
}