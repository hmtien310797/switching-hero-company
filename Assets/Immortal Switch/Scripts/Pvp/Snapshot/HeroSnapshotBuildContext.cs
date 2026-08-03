using System.Collections.Generic;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    /// <summary>
    /// Context cho <see cref="HeroBattleSnapshotBuilder"/>. Mang Stats (FinalStats source) +
    /// pre-built progression fragments + identity. Player: fragments do adapter đọc live managers;
    /// mock opponent (M4): fragments do generator tạo. Builder agnostic về nguồn fragments.
    /// </summary>
    public sealed class HeroSnapshotBuildContext
    {
        public int HeroId;
        public FormationSlot AssignedSlot;
        public int HeroDataVersion = 1;

        // Identity progression.
        public int Level;
        public int Star;
        public HeroProgressTier Tier;

        // FinalStats source — player HeroActor.Stats (đã có mọi modifier) hoặc mock-generated StatsController.
        // Nếu null, builder dùng <see cref="PrebuiltFinalStats"/> (mock opponent path — không cần live StatsController).
        public StatsController Stats;
        public HeroDataSO HeroData;

        /// <summary>FinalStats do caller dựng sẵn (mock opponent). Dùng khi <see cref="Stats"/> == null.</summary>
        public RuntimeStatSnapshot PrebuiltFinalStats;

        // Pre-built fragments.
        public List<FormationBuffSnapshot> FormationBuffs = new();
        public EquipmentSnapshot Equipment;
        public SkillProgressionSnapshot Skills;
        public GrowthSnapshot Growth;
        public TransmutationSnapshot Transmutation;
        public List<HeroPowerSourceSnapshot> AdditionalPowerSources = new();
        public List<BattlePassiveEffectSnapshot> PassiveEffects = new();
    }
}
