using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Pvp.Snapshot;

namespace Immortal_Switch.Scripts.Pvp.Mock
{
    /// <summary>
    /// Progression data của 1 mock hero (DOCX §30 — "MockHeroProgressionData"). Dùng để build
    /// HeroBattleSnapshot qua cùng pipeline snapshot với player. <b>FLAGGED:</b> DOCX không định
    /// nghĩa field; shape infer từ HeroBattleSnapshot sub-fragments.
    /// </summary>
    [Serializable]
    public sealed class MockHeroProgressionData
    {
        public int HeroId;
        public int Star;
        public int Tier;            // (int) HeroProgressTier
        public int Level;
        public RuntimeStatSnapshot FinalStats;
        public EquipmentSnapshot Equipment;
        public SkillProgressionSnapshot Skills;
        public GrowthSnapshot Growth;
        public TransmutationSnapshot Transmutation;
        public List<FormationBuffSnapshot> FormationBuffs = new();
    }
}
