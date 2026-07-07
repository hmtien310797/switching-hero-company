using System;
using UnityEngine;

namespace Battle.Dungeon
{
    [Serializable]
    public struct DungeonDpsThresholdFormula
    {
        [SerializeField] private DungeonFormulaData formula;
        [SerializeField, Min(0f)] private float rewardPercent;

        public DungeonFormulaData Formula => formula;
        public float RewardPercent => rewardPercent;
    }

    [Serializable]
    public sealed class DungeonDpsThresholdRow
    {
        [SerializeField] private string tableKey;
        [SerializeField, Min(1)] private int stage = 1;
        [SerializeField] private DungeonDpsThresholdFormula threshold1;
        [SerializeField] private DungeonDpsThresholdFormula threshold2;
        [SerializeField] private DungeonDpsThresholdFormula threshold3;

        public string TableKey => tableKey;
        public int Stage => stage;
        public DungeonDpsThresholdFormula Threshold1 => threshold1;
        public DungeonDpsThresholdFormula Threshold2 => threshold2;
        public DungeonDpsThresholdFormula Threshold3 => threshold3;
    }
}
