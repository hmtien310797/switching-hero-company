using System;
using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.Equipment.Definitions;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using UnityEngine;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon
{
    [CreateAssetMenu(fileName = "WeaponSummonConfig", menuName = "Game/Summon/Weapon Summon Config")]
    public class WeaponSummonConfigSO : ScriptableObject
    {
        [Header("Options")]
        public List<WeaponSummonOptionEntry> Options = new();

        [Header("Summon Levels")]
        public List<WeaponSummonLevelEntry> SummonLevels = new();

        [Header("Rewards")]
        public List<WeaponSummonLevelRewardEntry> LevelRewards = new();
        
        public WeaponSummonOptionEntry GetOption(string optionId)
        {
            return Options.FirstOrDefault(x => x != null && x.OptionId == optionId);
        }

        public WeaponSummonLevelEntry GetExactLevelEntry(int level)
        {
            return SummonLevels.FirstOrDefault(x => x != null && x.SummonLevel == level);
        }

        public WeaponSummonLevelRewardEntry GetRewardEntry(int level)
        {
            return LevelRewards.FirstOrDefault(x => x != null && x.SummonLevel == level);
        }

        private StandardWeaponDefinitionSO GetWeaponById(List<StandardWeaponDefinitionSO> weaponPool, int weaponId)
        {
            return weaponPool.FirstOrDefault(x => x != null && x.WeaponId == weaponId);
        }

        private StandardWeaponDefinitionSO GetWeaponByName(List<StandardWeaponDefinitionSO> weaponPool, string weaponName)
        {
            return weaponPool.FirstOrDefault(x => x != null && x.WeaponName == weaponName);
        }

        public StandardWeaponDefinitionSO GetWeapon(List<StandardWeaponDefinitionSO> weaponPool, int weaponId, string weaponName = null)
        {
            var byId = GetWeaponById(weaponPool, weaponId);
            if (byId != null) return byId;
            if (!string.IsNullOrEmpty(weaponName))
            {
                var byName = GetWeaponByName(weaponPool, weaponName);
                if (byName != null) { Debug.LogWarning($"[WeaponSummonConfig] Weapon id={weaponId} not found, fell back to name='{weaponName}'"); return byName; }
            }
            return null;
        }
    }

    [Serializable]
    public class WeaponSummonOptionEntry
    {
        public string OptionId;
        public bool Enabled = true;
        public int RollCount = 1;
        public int TicketCost;
        public int GemCost;
    }

    [Serializable]
    public class WeaponSummonLevelEntry
    {
        public int SummonLevel = 1;
        public int TotalRollRequired = 10;

        [Header("Tier Rate")]
        public float GradeDRate;
        public float GradeCRate;
        public float GradeBRate;
        public float GradeARate;
        public float GradeSRate;
        public float GradeSSRate;

        [Header("Star Rate")]
        public float Star1Rate = 45f;
        public float Star2Rate = 30f;
        public float Star3Rate = 15f;
        public float Star4Rate = 8f;
        public float Star5Rate = 2f;

        [Header("Milestone Reward")]
        public int ItemId;
        public int ItemQuantity;
    }

    [Serializable]
    public class WeaponSummonLevelRewardEntry
    {
        public int SummonLevel;
        public List<SummonRewardItem> RewardItems = new();
    }
}