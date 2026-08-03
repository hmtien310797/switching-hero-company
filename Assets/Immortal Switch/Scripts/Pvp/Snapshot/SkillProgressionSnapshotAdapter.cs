using System;
using Common;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    /// <summary>
    /// Adapter đọc skill loadout của 1 hero từ UserDataCache.SkillList.Equipped (heroUid → skillUid[]).
    /// DOCX §9 — Skills fragment. Resolve skillUid → SkillInstance (SkillId/Level/Grade).
    /// </summary>
    public static class SkillProgressionSnapshotAdapter
    {
        public static SkillProgressionSnapshot Build(int heroId)
        {
            var snap = new SkillProgressionSnapshot { HeroId = heroId };
            try
            {
                var cache = UserDataCache.Instance;
                if (cache == null) return snap;

                string heroUid = cache.GetHeroUid(heroId);
                snap.HeroUid = heroUid ?? string.Empty;

                var equipped = cache.SkillList?.Equipped;
                if (equipped == null || string.IsNullOrEmpty(heroUid)) return snap;
                if (!equipped.TryGetValue(heroUid, out var skillUids) || skillUids == null) return snap;

                var owned = cache.SkillList?.Owned;
                if (owned == null) return snap;

                foreach (var suid in skillUids)
                {
                    if (string.IsNullOrEmpty(suid)) continue;
                    var inst = Array.Find(owned, s => s != null && s.Uid == suid);
                    if (inst == null) continue;
                    snap.Equipped.Add(new SkillSlotSnapshot
                    {
                        SkillId = inst.SkillId,
                        Level = inst.Level,
                        Grade = inst.Grade
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PvP] SkillProgressionSnapshotAdapter({heroId}): {e.Message}");
            }
            return snap;
        }
    }
}
