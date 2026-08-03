using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Battle
{
    /// <summary>
    /// Per-hero summary (DOCX §35). Snapshot final HP/dead + damage/heal/shield + attack/skill/ult
    /// counts + buff trigger counts. CombatLogHash ở <see cref="PvPBattleResultRequest"/>.
    /// </summary>
    [Serializable]
    public sealed class HeroBattleResultSummary
    {
        public int HeroId;
        public FormationSlot AssignedSlot;
        public long FinalHp;
        public bool IsDead;

        public long DamageDealt;
        public long DamageReceived;
        public long HealingDone;
        public long ShieldGenerated;

        public int BasicAttackCount;
        public int ClassSkillCastCount;
        public int UltimateCastCount;

        public Dictionary<string, int> BuffTriggerCounts = new();
    }
}
