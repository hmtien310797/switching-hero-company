using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    [CreateAssetMenu(menuName = "Growth/Growth Stat Visual Database")]
    public class GrowthStatVisualDatabaseSO : ScriptableObject
    {
        public StatVisualEntry[] Entries;

        public StatVisualEntry GetEntry(StatType stat)
        {
            if (Entries == null) return default;

            for (int i = 0; i < Entries.Length; i++)
            {
                if (Entries[i].Stat == stat)
                    return Entries[i];
            }

            return default;
        }
    }

    [System.Serializable]
    public struct StatVisualEntry
    {
        public StatType Stat;
        public string DisplayName;
        public Sprite Icon;
    }
}