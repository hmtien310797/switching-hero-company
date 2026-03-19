using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    [CreateAssetMenu(menuName = "Growth/Growth Stat UI Database")]
    public class GrowthStatUIViewDatabaseSO : ScriptableObject
    {
        public StatUIEntry[] entries;

        public StatUIEntry Get(StatType stat)
        {
            if (entries == null) return default;

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Stat == stat)
                    return entries[i];
            }

            return default;
        }
    }

    [System.Serializable]
    public struct StatUIEntry
    {
        public StatType Stat;
        public string DisplayName;
        public Sprite Icon;
    }
}