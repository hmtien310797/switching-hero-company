using System;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Repositories;

namespace Immortal_Switch.Scripts.Pvp.Services
{
    /// <summary>
    /// Phase-1 buff inventory service (DOCX §38, §17). Sở hữu ownership records + equip/unequip.
    /// One BuffId chỉ equipped 1 vị trí — kiểm tra ở CanEquip. UI gọi interface, không đụng ES3.
    /// </summary>
    internal sealed class LocalPvpBuffInventoryService : IPvPBuffInventoryService
    {
        private readonly IPvPRepository _repo;
        private readonly IFormationBuffCatalogService _catalog;

        public LocalPvpBuffInventoryService(IPvPRepository repo, IFormationBuffCatalogService catalog)
        {
            _repo = repo;
            _catalog = catalog;
        }

        public PvpOwnedBuffData LoadOwnedBuffs()
        {
            return _repo.Load<PvpOwnedBuffData>(PvpEs3Keys.OwnedBuffs);
        }

        public PvpOwnedBuff GetOwnedBuff(string buffId)
        {
            if (string.IsNullOrEmpty(buffId)) return null;
            var data = LoadOwnedBuffs();
            return data.Buffs.Find(b => b.BuffId == buffId);
        }

        public bool IsOwned(string buffId)
        {
            return GetOwnedBuff(buffId)?.IsUnlocked == true;
        }

        public bool IsEquipped(PvpFormationSaveData formation, string buffId)
        {
            if (formation == null || string.IsNullOrEmpty(buffId)) return false;
            return BuffIdInLoadout(formation.FrontLoadout, buffId)
                || BuffIdInLoadout(formation.BackLoadout, buffId);
        }

        public bool CanEquip(PvpFormationSaveData formation, string buffId, FormationSlot slot,
            BuffSlotType slotType, out string conflictReason)
        {
            conflictReason = null;

            if (string.IsNullOrEmpty(buffId))
            {
                conflictReason = "Empty buff id.";
                return false;
            }

            if (!IsOwned(buffId))
            {
                conflictReason = $"Buff '{buffId}' is not owned.";
                return false;
            }

            if (!_catalog.TryGet(buffId, out var buff))
            {
                conflictReason = $"Buff '{buffId}' is not in catalog.";
                return false;
            }

            if (buff.SlotType != slotType)
            {
                conflictReason = $"'{buffId}' is a {buff.SlotType} buff; cannot place in {slotType} slot.";
                return false;
            }

            switch (buff.FormationSlotCompat)
            {
                case FormationBuffSlotCompat.FrontOnly when slot == FormationSlot.Back:
                    conflictReason = $"'{buffId}' is FrontOnly; cannot equip on Back.";
                    return false;
                case FormationBuffSlotCompat.BackOnly when slot == FormationSlot.Front:
                    conflictReason = $"'{buffId}' is BackOnly; cannot equip on Front.";
                    return false;
            }

            if (IsEquippedElsewhere(formation, buffId, slot, slotType))
            {
                conflictReason = $"'{buffId}' is already equipped on another position; one BuffId may only be equipped on one position (DOCX §17).";
                return false;
            }

            return true;
        }

        public void Equip(PvpFormationSaveData formation, string buffId, FormationSlot slot, BuffSlotType slotType)
        {
            if (!CanEquip(formation, buffId, slot, slotType, out var reason))
                throw new InvalidOperationException($"Cannot equip '{buffId}': {reason}");

            var loadout = slot == FormationSlot.Front ? formation.FrontLoadout : formation.BackLoadout;
            loadout.Set(slotType, buffId);
        }

        public bool Unequip(PvpFormationSaveData formation, FormationSlot slot, BuffSlotType slotType)
        {
            var loadout = slot == FormationSlot.Front ? formation.FrontLoadout : formation.BackLoadout;
            string current = loadout.Get(slotType);
            if (string.IsNullOrEmpty(current)) return false;
            loadout.Set(slotType, null);
            return true;
        }

        public void GrantOwnership(string buffId, int level, int shards)
        {
            var data = LoadOwnedBuffs();
            var existing = data.Buffs.Find(b => b.BuffId == buffId);
            if (existing == null)
            {
                existing = new PvpOwnedBuff { BuffId = buffId };
                data.Buffs.Add(existing);
            }
            existing.IsUnlocked = true;
            existing.Level = level;
            existing.Shards = shards;
            _repo.Save(PvpEs3Keys.OwnedBuffs, data);
        }

        public void AddShards(string buffId, int amount)
        {
            if (amount <= 0) return;
            var data = LoadOwnedBuffs();
            var existing = data.Buffs.Find(b => b.BuffId == buffId);
            if (existing == null)
            {
                existing = new PvpOwnedBuff { BuffId = buffId };
                data.Buffs.Add(existing);
            }
            existing.Shards += amount;
            _repo.Save(PvpEs3Keys.OwnedBuffs, data);
        }

        public void SetLevel(string buffId, int level)
        {
            var data = LoadOwnedBuffs();
            var existing = data.Buffs.Find(b => b.BuffId == buffId);
            if (existing == null) return;
            existing.Level = level;
            _repo.Save(PvpEs3Keys.OwnedBuffs, data);
        }

        public void SaveOwnedBuffs(PvpOwnedBuffData data)
        {
            _repo.Save(PvpEs3Keys.OwnedBuffs, data ?? new PvpOwnedBuffData());
        }

        private static bool BuffIdInLoadout(PvpBuffSlotLoadout loadout, string buffId)
        {
            if (loadout == null) return false;
            return loadout.Core == buffId || loadout.Support == buffId || loadout.Trigger == buffId;
        }

        /// <summary>
        /// True nếu buffId xuất hiện ở bất kỳ slot nào KHÁC (slot, slotType) đang target —
        /// bao gồm slot khác trong cùng loadout (cùng vị trí) hoặc bất kỳ slot ở vị trí khác.
        /// </summary>
        private static bool IsEquippedElsewhere(PvpFormationSaveData formation, string buffId,
            FormationSlot targetSlot, BuffSlotType targetSlotType)
        {
            bool FoundIn(PvpBuffSlotLoadout loadout, FormationSlot slot)
            {
                if (loadout == null) return false;

                if (slot == targetSlot)
                {
                    // Cùng vị trí: chỉ conflict nếu buff đang ở slot-type KHÁC slotType target.
                    return (loadout.Core == buffId && targetSlotType != BuffSlotType.Core)
                        || (loadout.Support == buffId && targetSlotType != BuffSlotType.Support)
                        || (loadout.Trigger == buffId && targetSlotType != BuffSlotType.Trigger);
                }

                // Vị trí khác: bất kỳ slot chứa buffId đều là conflict.
                return BuffIdInLoadout(loadout, buffId);
            }

            return FoundIn(formation.FrontLoadout, FormationSlot.Front)
                || FoundIn(formation.BackLoadout, FormationSlot.Back);
        }
    }
}
