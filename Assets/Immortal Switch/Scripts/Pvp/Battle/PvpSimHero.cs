using System.Collections.Generic;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Snapshot;

namespace Immortal_Switch.Scripts.Pvp.Battle
{
    /// <summary>
    /// Runtime hero state cho Phase-1 simulation. Public fields để <see cref="FormationBuffRuntime"/>
    /// truy cập. §16 cleanup = thuần local (GC-able sau Run). <b>FLAGGED:</b> sim-only state, không
    /// phải live <c>ICombatUnit</c> — real-time engine sẽ dùng HeroActor + StatsController.
    /// </summary>
    public sealed class PvpSimHero
    {
        public int HeroId;
        public FormationSlot Slot;
        public float MaxHp, CurrentHp;
        public float Atk, Def, CritChance, CritDamage, AttackSpeed;
        public bool IsDead;
        public float ActionCharge;
        public float ClassSkillCd, UltimateCd;
        public int BasicAttackCount, ClassSkillCount, UltimateCount;
        public long DamageDealt, DamageReceived, HealingDone, ShieldGenerated;
        public float Shield;   // current shield (absorbs damage — DOCX §18 Shield effect)
        public List<FormationBuffSnapshot> FormationBuffs = new();
        public Dictionary<string, int> BuffTriggerCounts = new();
        public Dictionary<string, BuffTriggerState> BuffStates = new();
    }

    /// <summary>Runtime state per buff per hero: cooldown/trigger-count for limits (DOCX §18 Runtime).</summary>
    public sealed class BuffTriggerState
    {
        public float CooldownRemaining;
        public int TriggerCount;
        public bool TriggeredOnceThisBattle;
    }
}
