using System;
using System.Collections.Generic;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill.UI
{
    [Serializable]
    public class SkillProgressSaveData
    {
        public int SkillId;
        public bool IsOwned;
        public int Level = 1;
        public int CurrentShard;
        public int RequiredShard = 2;
    }

    public static class SkillInventorySaveService
    {
        private const string SaveFile = "SkillInventory.es3";
        private const string AllProgressKey = "all_skill_progress";

        private static Dictionary<int, SkillProgressSaveData> cache;

        private static void EnsureLoaded()
        {
            if (cache != null)
                return;

            cache = ES3.Load(AllProgressKey, SaveFile, new Dictionary<int, SkillProgressSaveData>());
            if (cache == null)
                cache = new Dictionary<int, SkillProgressSaveData>();
        }

        private static void SaveAll()
        {
            EnsureLoaded();
            ES3.Save(AllProgressKey, cache, SaveFile);
        }

        public static SkillProgressSaveData GetOrCreate(int skillId)
        {
            EnsureLoaded();

            if (skillId <= 0)
                return new SkillProgressSaveData();

            if (!cache.TryGetValue(skillId, out var data) || data == null)
            {
                data = new SkillProgressSaveData
                {
                    SkillId = skillId,
                    IsOwned = false,
                    Level = 1,
                    CurrentShard = 0,
                    RequiredShard = 2
                };
                cache[skillId] = data;
                SaveAll();
            }

            return data;
        }

        public static bool IsOwned(int skillId) => GetOrCreate(skillId).IsOwned;
        public static int GetLevel(int skillId) => Mathf.Max(1, GetOrCreate(skillId).Level);
        public static int GetCurrentShard(int skillId) => Mathf.Max(0, GetOrCreate(skillId).CurrentShard);
        public static int GetRequiredShard(int skillId) => Mathf.Max(1, GetOrCreate(skillId).RequiredShard);

        public static void SetOwned(int skillId, bool isOwned)
        {
            var data = GetOrCreate(skillId);
            data.IsOwned = isOwned;
            SaveAll();
        }

        public static void SetLevel(int skillId, int level)
        {
            var data = GetOrCreate(skillId);
            data.Level = Mathf.Max(1, level);
            SaveAll();
        }

        public static void SetShard(int skillId, int currentShard, int requiredShard)
        {
            var data = GetOrCreate(skillId);
            data.CurrentShard = Mathf.Max(0, currentShard);
            data.RequiredShard = Mathf.Max(1, requiredShard);
            SaveAll();
        }

        public static void AddShard(int skillId, int amount, int requiredShard = 2)
        {
            var data = GetOrCreate(skillId);
            data.CurrentShard = Mathf.Max(0, data.CurrentShard + amount);
            data.RequiredShard = Mathf.Max(1, requiredShard);

            if (data.CurrentShard > 0)
                data.IsOwned = true;

            SaveAll();
        }

        public static void ClearAll()
        {
            cache = new Dictionary<int, SkillProgressSaveData>();
            if (ES3.KeyExists(AllProgressKey, SaveFile))
                ES3.DeleteKey(AllProgressKey, SaveFile);
        }
    }
}