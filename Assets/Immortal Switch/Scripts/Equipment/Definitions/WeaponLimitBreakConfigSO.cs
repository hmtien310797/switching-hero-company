using System;
using System.Collections.Generic;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.Definitions
{
    [CreateAssetMenu(fileName = "WeaponLimitBreakConfig", menuName = "ScriptableObjects/Equipment/WeaponLimitBreakConfig")]
    public class WeaponLimitBreakConfigSO : ScriptableObject
    {
        public string ConfigId;

        public int LevelPerStage = 25;

        public List<WeaponLimitBreakEntry> Entries;

        public WeaponLimitBreakEntry GetEntry(int nextStage)
        {
            return Entries.Find(x => x.Stage == nextStage);
        }

        public int GetMaxLevel(int currentStage)
        {
            return (currentStage + 1) * LevelPerStage;
        }
        
        public WeaponLimitBreakEntry GetEntryByStage(int stage)
        {
            return Entries.Find(x => x.Stage == stage);
        }
    }

    [Serializable]
    public class WeaponLimitBreakEntry
    {
        public int Stage;
        public int RequiredLevel;
        public int BreakThroughStoneCost;
        public float SuccessRate;
    }
}