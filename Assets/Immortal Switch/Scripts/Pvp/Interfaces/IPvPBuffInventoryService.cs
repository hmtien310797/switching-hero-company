using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Interfaces
{
    /// <summary>
    /// Buff inventory service (DOCX §38 — IPvPBuffInventoryService). Sở hữu ownership records
    /// (owned/level/shards) + equip/unequip (mutate formation loadout). One BuffId chỉ equipped
    /// 1 vị trí (DOCX §17). Gacha/upgrade (M7) dùng GrantOwnership/AddShards/SetLevel.
    /// </summary>
    public interface IPvPBuffInventoryService
    {
        PvpOwnedBuffData LoadOwnedBuffs();
        PvpOwnedBuff GetOwnedBuff(string buffId);
        bool IsOwned(string buffId);

        /// <summary>True nếu buffId đang được equip ở bất kỳ slot nào trong formation.</summary>
        bool IsEquipped(PvpFormationSaveData formation, string buffId);

        /// <summary>
        /// Check equip: owned + chưa equipped elsewhere + slot-type matching + FrontOnly/BackOnly
        /// compat. Trả <paramref name="conflictReason"/> nếu fail (Markdown screen 04 — conflict msg).
        /// </summary>
        bool CanEquip(PvpFormationSaveData formation, string buffId, FormationSlot slot,
            BuffSlotType slotType, out string conflictReason);

        /// <summary>Mutate formation loadout: set buff vào slot. Caller chịu trách nhiệm save formation.</summary>
        void Equip(PvpFormationSaveData formation, string buffId, FormationSlot slot, BuffSlotType slotType);

        /// <summary>Mutate formation loadout: clear slot. Trả true nếu có buff bị gỡ.</summary>
        bool Unequip(PvpFormationSaveData formation, FormationSlot slot, BuffSlotType slotType);

        // ── Ownership mutation (gacha/upgrade dùng — M7) ──
        void GrantOwnership(string buffId, int level, int shards);
        void AddShards(string buffId, int amount);
        void SetLevel(string buffId, int level);
        void SaveOwnedBuffs(PvpOwnedBuffData data);
    }
}
