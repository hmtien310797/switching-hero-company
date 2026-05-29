using System;
using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Immortal_Switch.Scripts.MissionSystem.Models
{
    [CreateAssetMenu(fileName = "MissionSystemDatabase", menuName = "ScriptableObjects/MissionSystem/Database")]
    public class MissionSystemDatabaseSO : ScriptableObject
    {
        /// <summary>
        /// ds nhiem vu.
        /// </summary>
        [SerializeField] private MissionEntry[] missions;

        /// <summary>
        /// mission dict
        /// </summary>
        private readonly Dictionary<int, MissionEntry> _missions = new();

        public void Load()
        {
            _missions.Clear();

            foreach (var entry in missions.OrderBy(v => v.id))
            {
                _missions.TryAdd(entry.id, entry);
            }
        }

        public KeyValuePair<int, MissionEntry> FirstMission => _missions.ElementAtOrDefault(0);

        public MissionEntry? GetEntry(int id)
        {
            return _missions.TryGetValue(id, out var entry) ? entry : null;
        }

        public MissionEntry? NextEntry(int? currentId)
        {
            if (currentId == null)
            {
                return missions[0];
            }

            var idx = Array.FindIndex(missions, 0, v => v.id == currentId);

            if (idx < 0)
            {
                return missions[0];
            }

            // lay ra nhiem vu tiep theo.
            return idx + 1 >= missions.Length ? null : missions[idx + 1];
        }
    }

    [Serializable]
    public struct MissionEntry
    {
        /// <summary>
        /// id nhiem vu
        /// </summary>
        public int id;

        /// <summary>
        /// ten nhiem vu localize.
        /// </summary>
        public string title;

        /// <summary>
        /// cac params field tuong ung voi title.
        /// </summary>
        public string[] titleParams;

        /// <summary>
        /// loai nhiem vu
        /// </summary>
        public EMissionSystemType type;

        /// <summary>
        /// Mục tiêu cần đạt.
        /// </summary>
        public int target;

        /// <summary>
        /// phan thuong.
        /// </summary>
        public RewardEntry reward;

        /// <summary>
        /// Title sau khi format.
        /// </summary>
        [JsonIgnore]
        public string FormatTitle =>
            string.Format(
                title,
                titleParams.Cast<object>().ToArray()
            );
    }
}