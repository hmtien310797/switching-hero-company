using System;
using Immortal_Switch.Scripts.TransmutationSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    /// <summary>
    /// Adapter đọc transmutation state player-wide từ TransmutationSystemManager.Storage.Data.
    /// DOCX §9 — Transmutation fragment. Exp/Crystal (BigInteger) → string.
    /// </summary>
    public static class TransmutationSnapshotAdapter
    {
        public static TransmutationSnapshot Build()
        {
            var snap = new TransmutationSnapshot();
            try
            {
                var data = TransmutationSystemManager.Instance?.Storage?.Data;
                if (data == null) return snap;

                snap.Level = data.Level;
                snap.Exp = data.Exp.ToString();
                snap.Crystal = data.Crystal.ToString();

                if (data.Equips == null) return snap;
                foreach (var kv in data.Equips)
                {
                    if (kv.Value == null) continue;
                    snap.Equips.Add(new TransmutationEquipSnapshot
                    {
                        ItemType = kv.Key,
                        CfgId = kv.Value.CfgId,
                        Tier = kv.Value.Tier,
                        Level = kv.Value.Level
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PvP] TransmutationSnapshotAdapter: {e.Message}");
            }
            return snap;
        }
    }
}
