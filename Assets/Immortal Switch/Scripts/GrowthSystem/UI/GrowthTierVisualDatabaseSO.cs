using UnityEngine;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    [CreateAssetMenu(menuName = "Growth/Growth Tier Visual Database")]
    public class GrowthTierVisualDatabaseSO : ScriptableObject
    {
        public TierVisualEntry[] Entries;

        public Sprite GetIconByTier(int tier)
        {
            if (Entries == null || Entries.Length == 0)
                return null;

            int groupIndex = GetTierGroupIndex(tier);

            for (int i = 0; i < Entries.Length; i++)
            {
                if (Entries[i].TierGroupIndex == groupIndex)
                    return Entries[i].Icon;
            }

            return null;
        }

        public static int GetTierGroupIndex(int tier)
        {
            if (tier <= 0) return 0;
            return (tier - 1) / 10;
        }
    }

    [System.Serializable]
    public struct TierVisualEntry
    {
        public int TierGroupIndex; // 0 = tier 1-10, 1 = tier 11-20, ...
        public Sprite Icon;
    }
}