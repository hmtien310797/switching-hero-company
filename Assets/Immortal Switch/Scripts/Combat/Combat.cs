using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Combat
{
    public static class DamageCalculator
    {
        public static DamageResult CalculateDamage(
            ICombatUnit attacker,
            ICombatUnit defender,
            float skillCoefficient = 0)
        {
            DamageResult result = new DamageResult();

            float baseAtk = attacker.Stats.StatModule.GetFinalStat(StatType.Atk);
            float flatAtkBonus = attacker.Stats.StatModule.GetFinalStat(StatType.FlatAtkBonus);
            float atkPercentBonus = attacker.Stats.StatModule.GetFinalStat(StatType.AtkPercentBonus);

            float enemyDef = defender.Stats.StatModule.GetFinalStat(StatType.Def);

            float critChance = attacker.Stats.StatModule.GetFinalStat(StatType.CritChance);
            float critDamage = attacker.Stats.StatModule.GetFinalStat(StatType.CritDamage);

            bool isCrit = UnityEngine.Random.value < critChance;
            float critMultiplier = isCrit ? critDamage : 1f;

            float defenseMultiplier = 100f / (100f + enemyDef);
            float calculatedSkillCo = skillCoefficient/100;

            float atk1 =
                baseAtk *
                (1 + calculatedSkillCo) *
                defenseMultiplier *
                critMultiplier;

            float finalDamage =
                (atk1 + flatAtkBonus) *
                (1f + atkPercentBonus);

            result.Damage = finalDamage;
            result.DamageType = isCrit ? DamageType.Crit : DamageType.Normal;

            result.BaseATK = baseAtk;
            result.SkillCoefficient = skillCoefficient;
            result.DefenseMultiplier = defenseMultiplier;
            result.FlatATKBonus = flatAtkBonus;
            result.ATKPercentBonus = atkPercentBonus;

            return result;
        }
    }
}