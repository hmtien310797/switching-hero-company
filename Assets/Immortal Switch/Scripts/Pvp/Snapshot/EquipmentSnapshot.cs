using System;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    /// <summary>
    /// Equipment fragment (DOCX §9). <b>FLAGGED:</b> field shapes inferred từ WeaponModels
    /// (HeroWeaponEquipEntry / StandardWeaponState / ExclusiveWeaponState) — không do DOCX định nghĩa.
    /// </summary>
    [Serializable]
    public sealed class EquipmentSnapshot
    {
        public int HeroId;
        public int StandardWeaponId = -1;
        public int StandardWeaponLevel;
        public int StandardWeaponLimitBreak;
        public int ExclusiveWeaponId = -1;
        public int ExclusiveWeaponLevel;
        public int ExclusiveWeaponStar;
        public bool UseExclusive;
    }
}
