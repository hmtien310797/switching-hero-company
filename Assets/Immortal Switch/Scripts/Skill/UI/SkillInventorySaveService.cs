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

        // Legacy field: giữ lại để không vỡ dữ liệu cũ.
        public int RequiredShard = 2;
    }

    public static class SkillInventorySaveService
    {
        private static Dictionary<int, SkillProgressSaveData> cache;

        private static void EnsureLoaded()
        {
            if (cache != null)
                return;
            
            if (cache == null)
                cache = new Dictionary<int, SkillProgressSaveData>();
        }

        private static void SaveAll()
        {
            EnsureLoaded();
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

        public static void SetCurrentShard(int skillId, int currentShard)
        {
            var data = GetOrCreate(skillId);
            data.CurrentShard = Mathf.Max(0, currentShard);
            SaveAll();
        }

        public static void AddShard(int skillId, int amount)
        {
            if (skillId <= 0 || amount <= 0)
                return;

            var data = GetOrCreate(skillId);
            data.CurrentShard = Mathf.Max(0, data.CurrentShard + amount);

            if (data.CurrentShard > 0)
                data.IsOwned = true;

            SaveAll();
        }

        public static List<SkillProgressSaveData> GetAllSkillStates()
        {
            EnsureLoaded();

            var result = new List<SkillProgressSaveData>();

            foreach (var kv in cache)
            {
                if (kv.Value != null)
                    result.Add(kv.Value);
            }

            return result;
        }

        public static void Save()
        {
            SaveAll();
        }
        
    }
}