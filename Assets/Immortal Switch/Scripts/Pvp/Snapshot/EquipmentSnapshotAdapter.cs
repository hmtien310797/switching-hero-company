using System;
using Immortal_Switch.Scripts.Equipment.Core;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    /// <summary>
    /// Adapter đọc equipment state của 1 hero từ WeaponManager.SaveData (FLAGGED: read-only, tránh
    /// GetOrCreate side-effect). DOCX §9 — Equipment fragment.
    /// </summary>
    public static class EquipmentSnapshotAdapter
    {
        public static EquipmentSnapshot Build(int heroId)
        {
            var snap = new EquipmentSnapshot { HeroId = heroId };
            try
            {
                var save = WeaponManager.Instance?.SaveData;
                if (save == null) return snap;

                var equip = save.HeroEquips?.Find(x => x != null && x.HeroId == heroId);
                if (equip == null) return snap;

                snap.UseExclusive = equip.UseExclusive;

                if (equip.EquippedStandardWeaponId > 0)
                {
                    snap.StandardWeaponId = equip.EquippedStandardWeaponId;
                    var std = save.StandardWeapons?.Find(
                        x => x != null && x.WeaponId == equip.EquippedStandardWeaponId);
                    if (std != null)
                    {
                        snap.StandardWeaponLevel = std.Level;
                        snap.StandardWeaponLimitBreak = std.LimitBreakStage;
                    }
                }

                if (equip.EquippedExclusiveWeaponId > 0)
                {
                    snap.ExclusiveWeaponId = equip.EquippedExclusiveWeaponId;
                    var exc = save.ExclusiveWeapons?.Find(
                        x => x != null && x.ExclusiveWeaponId == equip.EquippedExclusiveWeaponId);
                    if (exc != null)
                    {
                        snap.ExclusiveWeaponLevel = exc.Level;
                        snap.ExclusiveWeaponStar = exc.CurrentStar;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PvP] EquipmentSnapshotAdapter({heroId}): {e.Message}");
            }
            return snap;
        }
    }
}
