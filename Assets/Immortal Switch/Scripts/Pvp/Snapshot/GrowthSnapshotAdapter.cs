using System;
using Immortal_Switch.Scripts.GrowthSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    /// <summary>
    /// Adapter đọc growth state player-wide từ GrowthManager.SaveData (CurrentUnlockedTier +
    /// per-stat stacks). DOCX §9 — Growth fragment.
    /// </summary>
    public static class GrowthSnapshotAdapter
    {
        public static GrowthSnapshot Build()
        {
            var snap = new GrowthSnapshot();
            try
            {
                var save = GrowthManager.Instance?.SaveData;
                if (save == null) return snap;

                snap.CurrentUnlockedTier = save.CurrentUnlockedTier;
                if (save.Stats == null) return snap;

                foreach (var sp in save.Stats)
                    snap.Stats.Add(new GrowthStatStackSnapshot
                    {
                        Stat = sp.Stat,
                        CurrentStack = sp.CurrentStack
                    });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PvP] GrowthSnapshotAdapter: {e.Message}");
            }
            return snap;
        }
    }
}
