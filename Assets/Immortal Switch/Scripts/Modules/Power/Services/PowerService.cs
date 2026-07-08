using System;
using System.Linq;
using Common;
using Immortal_Switch.Scripts.Modules.Power.Services.Interfaces;
using Immortal_Switch.Scripts.PowerUpSystem;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Modules.Power.Services
{
    internal class PowerService : IPowerService
    {
        public double CalculatePlayerCp()
        {
            var stats = PowerUpManager.Instance.BoundPlayerStats;
            var synergy = UserDataCache.Instance.AreAllBattleHeroesSameClass() ? ValueConstants.SYNERGY_MULT : 0f;
            var playerLevelInfo = DatabaseManager.Instance.GetLevelByTotalExp(UserDataCache.Instance.Exp);
            var totalHeroCp = stats.Sum(stat => CalculateHeroCp(stat, playerLevelInfo.level));
            var playerCp = totalHeroCp * (1 + synergy);
            return playerCp;
        }

        /// <summary>
        /// tinh toan cp step 1: OFFENSE_BASE = ATK × 14 + ATK × (SPD − 1) × 8
        /// </summary>
        private float CalculateStep1(StatsController stats)
        {
            var atk = stats.StatModule.GetFinalStat(StatType.Atk);
            var spd = stats.StatModule.GetFinalStat(StatType.AttackSpeed);
            return atk * 14 + atk * (spd - 1) * 8;
        }

        /// <summary>
        /// tinh toan cp step 2: OFFENSE = OFFENSE_BASE × (1 + CRIT_RATE × (CRIT_DMG − 1)) × 1.0
        /// </summary>
        private float CalculateStep2(StatsController stats, float step1)
        {
            var critRate = stats.StatModule.GetFinalStat(StatType.CritChance);
            var critDmg = stats.StatModule.GetFinalStat(StatType.CritDamage);
            return step1 * (1 + critRate * (critDmg - 1)) * 1.0f;
        }

        /// <summary>
        /// tinh toan cp step 3: OFFENSE = OFFENSE_STEP_2 × (1 + DMG_BONUS + 0.5×BASIC_DMG_BONUS + 0.5×SKILL_DMG_BONUS) × (1 + FINAL_DMG_BONUS)
        /// </summary>
        private float CalculateStep3(StatsController stats, float step2)
        {
            var dmgBonus = stats.StatModule.GetFinalStat(StatType.DmgBonus);
            var basicDmgBonus = stats.StatModule.GetFinalStat(StatType.BasicDmgBonus);
            var skillDmgBonus = stats.StatModule.GetFinalStat(StatType.SkillDmgBonus);
            var finalDmgBonus = stats.StatModule.GetFinalStat(StatType.FinalDmgBonus);
            return step2 * (1 + dmgBonus + 0.5f * basicDmgBonus + 0.5f * skillDmgBonus) * (1 + finalDmgBonus);
        }

        /// <summary>
        /// tinh toan cp step 4: DEFENSE = HP × 1 + DEF × 6
        /// </summary>
        private float CalculateStep4(StatsController stats)
        {
            var hp = stats.StatModule.GetFinalStat(StatType.MaxHp);
            var def = stats.StatModule.GetFinalStat(StatType.Def);
            return hp * 1 + def * 6;
        }

        /// <summary>
        /// tinh toan hero cp:  (OFFENSE + DEFENSE) × grade_mult × level_mult × 0.20
        /// level_mult = 1 + 0.85 × ln(player_level + 1)
        /// </summary>
        public double CalculateHeroCp(StatsController stats, int playerLevel)
        {
            var step1 = CalculateStep1(stats);
            var step2 = CalculateStep2(stats, step1);
            var step3 = CalculateStep3(stats, step2);
            var step4 = CalculateStep4(stats);
            var levelMult = CalculateLevelMult(playerLevel);
            return (step3 + step4) * levelMult * 0.2f;
        }

        private double CalculateLevelMult(int playerLevel)
        {
            return 1 + 0.85f * Math.Log(playerLevel + 1);
        }
    }
}