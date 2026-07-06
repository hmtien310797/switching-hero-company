using System;
using UnityEngine;

namespace Battle.Dungeon
{
    [Serializable]
    public sealed class DungeonDamageThresholdRow
    {
        [SerializeField] private string tableKey;
        [SerializeField, Min(1)] private int stage = 1;
        [SerializeField] private DungeonFormulaData requiredDamage;
        [SerializeField, Min(0f)] private float rewardMultiplierPercent = 100f;

        public string TableKey => tableKey;
        public int Stage => stage;
        public DungeonFormulaData RequiredDamage => requiredDamage;
        public float RewardMultiplierPercent => rewardMultiplierPercent;
    }
}
