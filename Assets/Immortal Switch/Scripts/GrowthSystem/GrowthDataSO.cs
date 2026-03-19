using System;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.GrowthSystem
{
    [CreateAssetMenu(menuName = "Growth/Growth Data")]
    public class GrowthDataSO : ScriptableObject
    {
        public int Tier;
        public int MaxStack;
        public StatGrowthData[] StatGrowths;
        
    }

    [Serializable]
    public struct StatGrowthData
    {
        public StatType Stat;
        public float ValuePerLevel;
        public int GoldCostPerLevel;
    }
}
