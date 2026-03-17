using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Combat
{
    public static class DamageCalculator
    {
        public static DamageResult CalculateDamage(
            ICombatUnit attacker,
            ICombatUnit defender,
            float skillCoefficient)
        {
            DamageResult result = new DamageResult();

            float baseAtk = attacker.Stats.StatModule.GetFinalStat(StatType.ATK);
            float flatAtkBonus = attacker.Stats.StatModule.GetFinalStat(StatType.FlatATKBonus);
            float atkPercentBonus = attacker.Stats.StatModule.GetFinalStat(StatType.ATKPercentBonus);

            float enemyDef = defender.Stats.StatModule.GetFinalStat(StatType.DEF);

            float critChance = attacker.Stats.StatModule.GetFinalStat(StatType.CritChance);
            float critDamage = attacker.Stats.StatModule.GetFinalStat(StatType.CritDamage);

            bool isCrit = UnityEngine.Random.value < critChance;
            float critMultiplier = isCrit ? critDamage : 1f;

            float defenseMultiplier = 100f / (100f + enemyDef);

            float atk1 =
                baseAtk *
                skillCoefficient *
                defenseMultiplier *
                critMultiplier;

            float finalDamage =
                (atk1 + flatAtkBonus) *
                (1f + atkPercentBonus);

            result.Damage = finalDamage;
            result.DamageTextType = isCrit ? DamageType.Crit : DamageType.Normal;

            result.BaseATK = baseAtk;
            result.SkillCoefficient = skillCoefficient;
            result.DefenseMultiplier = defenseMultiplier;
            result.FlatATKBonus = flatAtkBonus;
            result.ATKPercentBonus = atkPercentBonus;

            return result;
        }
    }
}