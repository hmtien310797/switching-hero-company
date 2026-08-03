using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    /// <summary>
    /// Snapshot đầy đủ của 1 hero trong battle (DOCX §9). FinalStats = combat input trực tiếp.
    /// Progression fragments (Equipment/Skills/Growth/Transmutation/AdditionalPowerSources) giữ cho
    /// debug/preview/mock verification/future server.
    /// <para><b>FLAGGED:</b> dùng <c>List&lt;T&gt;</c> thay <c>IReadOnlyList&lt;T&gt;</c> (DOCX dùng
    /// IReadOnlyList) để JSON round-trip được; server map ngược nếu cần.</para>
    /// </summary>
    [Serializable]
    public sealed class HeroBattleSnapshot
    {
        public int HeroId;
        public int HeroDataVersion = 1;
        public FormationSlot AssignedSlot;

        public int Level;
        public int Star;
        public HeroProgressTier Tier;

        public EquipmentSnapshot Equipment;
        public SkillProgressionSnapshot Skills;
        public GrowthSnapshot Growth;
        public TransmutationSnapshot Transmutation;
        public List<HeroPowerSourceSnapshot> AdditionalPowerSources = new();
        public RuntimeStatSnapshot FinalStats;
        public List<BattlePassiveEffectSnapshot> PassiveEffects = new();
        public List<FormationBuffSnapshot> FormationBuffs = new();
    }
}
