using System;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.GrowthSystem
{
    public class GrowthDebugStandalone : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GrowthDatabaseSO growthDatabase;

        [Header("Runtime")]
        [SerializeField] private int gold = 100000;
        [SerializeField] private StatType selectedStat = StatType.Atk;
        [SerializeField] private int customAmount = 1;
        [SerializeField] private int unlockTierTo = 1;

        [Header("Save Data (Runtime)")]
        [SerializeField] private GrowthSaveData saveData = new();

        private GrowthSystemService service;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            var prefab = Resources.Load<GameObject>("GrowthDebugger");

            if (prefab != null)
            {
                Instantiate(prefab);
            }
            else
            {
                Debug.LogWarning("Prefab not found in Resources!");
            }
        }
        
        private void Awake()
        {
            Rebuild();
        }

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            service = new GrowthSystemService(growthDatabase, saveData);
            Debug.Log("[GrowthDebugStandalone] Rebuilt.");
        }

        // =========================
        // UPGRADE
        // =========================

        [ContextMenu("Upgrade x1")]
        public void UpgradeX1() => Upgrade(1);

        [ContextMenu("Upgrade x10")]
        public void UpgradeX10() => Upgrade(10);

        [ContextMenu("Upgrade x100")]
        public void UpgradeX100() => Upgrade(100);

        [ContextMenu("Upgrade Custom")]
        public void UpgradeCustom() => Upgrade(customAmount);

        private void Upgrade(int amount)
        {
            Ensure();

            int beforeStack = service.GetCurrentStack(selectedStat);
            int beforeGold = gold;

            int bought = service.Upgrade(selectedStat, amount, ref gold);

            int afterStack = service.GetCurrentStack(selectedStat);

            Debug.Log(
                $"[Growth] Upgrade {selectedStat} x{amount} | Bought={bought}\n" +
                $"Stack: {beforeStack} -> {afterStack}\n" +
                $"Gold: {beforeGold} -> {gold}"
            );
        }

        // =========================
        // TIER
        // =========================

        [ContextMenu("Unlock Tier")]
        public void UnlockTier()
        {
            Ensure();

            int old = saveData.CurrentUnlockedTier;
            service.UnlockTier(unlockTierTo);

            Debug.Log($"[Growth] Tier: {old} -> {saveData.CurrentUnlockedTier}");
        }

        // =========================
        // INFO
        // =========================

        [ContextMenu("Log Selected Stat")]
        public void LogSelected()
        {
            Ensure();

            int stack = service.GetCurrentStack(selectedStat);
            int max = service.GetMaxAvailableStack(selectedStat);
            float value = service.GetTotalGrowthValue(selectedStat);
            float preview = service.GetNextLevelValuePreview(selectedStat, customAmount);
            int tier = service.GetCurrentTierOfStatProgress(selectedStat);
            int cost = service.GetUpgradeCost(selectedStat, customAmount);

            Debug.Log(
                $"[Growth] ===== {selectedStat} =====\n" +
                $"Tier: {tier} (Unlocked Tier: {saveData.CurrentUnlockedTier})\n" +
                $"Stack: {stack}/{max}\n" +
                $"Value: {Format(selectedStat, value)}\n" +
                $"Preview(+{customAmount}): {Format(selectedStat, preview)}\n" +
                $"Cost(+{customAmount}): {cost}\n" +
                $"Gold: {gold}"
            );
        }

        [ContextMenu("Log All Stats")]
        public void LogAll()
        {
            Ensure();

            Debug.Log($"[Growth] ===== ALL STATS | Tier={saveData.CurrentUnlockedTier} =====");

            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                if (!service.IsStatUnlocked(stat)) continue;

                int stack = service.GetCurrentStack(stat);
                int max = service.GetMaxAvailableStack(stat);
                float value = service.GetTotalGrowthValue(stat);

                Debug.Log(
                    $"{stat} | Stack {stack}/{max} | Value {Format(stat, value)}"
                );
            }
        }

        // =========================
        // GOLD
        // =========================

        [ContextMenu("Gold 1M")]
        public void Gold1M()
        {
            gold = 1_000_000;
            Debug.Log("[Growth] Gold = 1,000,000");
        }

        [ContextMenu("Gold MAX")]
        public void GoldMax()
        {
            gold = 999_999_999;
            Debug.Log("[Growth] Gold = MAX");
        }

        // =========================
        // RESET
        // =========================

        [ContextMenu("Reset")]
        public void ResetAll()
        {
            saveData = new GrowthSaveData();
            Rebuild();
            Debug.Log("[Growth] Reset done.");
        }

        // =========================
        // HELPER
        // =========================

        private void Ensure()
        {
            if (service == null)
                Rebuild();
        }

        private string Format(StatType stat, float value)
        {
            switch (stat)
            {
                case StatType.Atk:
                case StatType.MaxHp:
                case StatType.DamageToNormalMonster:
                case StatType.DamageToHeroMonster:
                case StatType.ClassSkillDamage:
                case StatType.ExclusiveSkillDamage:
                case StatType.SwitchSkillDamage:
                case StatType.AtkPercentBonus:
                case StatType.CritChance:
                case StatType.DamageReduction:
                    return $"{value * 100f:0.##}%";

                default:
                    return $"{value:0.##}";
            }
        }
    }
}