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
    
    public enum GrowthValueType
    {
        Percent = 0, 
        Flat = 1   
    }

    [Serializable]
    public struct StatGrowthData
    {
        public StatType Stat;
        public GrowthValueType ValueType;
        public float ValuePerLevel;
        public int GoldCostPerLevel;
    }
}
