using System;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.GrowthSystem
{
    [Serializable]
    public class GrowthDebugEntry
    {
        public StatType Stat;
        public int CurrentStack;
        public int MaxStack;

        public int CostX1;
        public int CostX10;
        public int CostX100;

        public int CanBuyX1;
        public int CanBuyX10;
        public int CanBuyX100;

        public bool IsMaxed;
    }
}