using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.GrowthSystem
{
    [Serializable]
    public class GrowthSaveData
    {
        public int CurrentUnlockedTier = 1;
        public List<GrowthStatProgress> Stats = new();

        public int GetStack(StatType stat)
        {
            for (int i = 0; i < Stats.Count; i++)
            {
                if (Stats[i].Stat == stat)
                    return Stats[i].CurrentStack;
            }

            return 0;
        }

        public void SetStack(StatType stat, int value)
        {
            for (int i = 0; i < Stats.Count; i++)
            {
                if (Stats[i].Stat == stat)
                {
                    var entry = Stats[i];
                    entry.CurrentStack = value;
                    Stats[i] = entry;
                    return;
                }
            }

            Stats.Add(new GrowthStatProgress
            {
                Stat = stat,
                CurrentStack = value
            });
        }

        public void AddStack(StatType stat, int amount)
        {
            SetStack(stat, GetStack(stat) + amount);
        }
    }

    [Serializable]
    public struct GrowthStatProgress
    {
        public StatType Stat;
        public int CurrentStack;
    }
}