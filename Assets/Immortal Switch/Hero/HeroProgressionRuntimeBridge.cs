using Immortal_Switch.Scripts.StatSystem;
using Scripts.Battle;
using UnityEngine;

namespace Immortal_Switch.Hero
{
    public class HeroProgressionRuntimeBridge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HeroDataSO heroData;
        [SerializeField] private PlayerHeroController playerHeroController;
        [SerializeField] private StatsController statsController;

        public HeroDataSO HeroData => heroData;
        public PlayerHeroController PlayerHeroController => playerHeroController;
        public StatsController StatsController => statsController;

        private void Awake()
        {
            if (playerHeroController == null)
                playerHeroController = GetComponent<PlayerHeroController>();

            if (statsController == null)
            {
                if (playerHeroController != null)
                    statsController = playerHeroController.Stats;
                else
                    statsController = GetComponentInChildren<StatsController>();
            }
        }

        public void Setup(HeroDataSO data, PlayerHeroController controller = null)
        {
            heroData = data;

            if (controller != null)
                playerHeroController = controller;

            if (playerHeroController != null && statsController == null)
                statsController = playerHeroController.Stats;
        }

        public void EnsureUnlocked()
        {
            if (heroData == null || HeroProgressionManager.Instance == null) return;

            if (!HeroProgressionManager.Instance.Service.HasHero(heroData.Id))
            {
                HeroProgressionManager.Instance.UnlockHero(heroData);
            }
        }

        public void RefreshFromProgression()
        {
            if (heroData == null)
            {
                Debug.LogWarning("HeroProgressionRuntimeBridge: missing HeroData");
                return;
            }

            if (statsController == null)
            {
                Debug.LogWarning("HeroProgressionRuntimeBridge: missing StatsController");
                return;
            }

            if (HeroProgressionManager.Instance == null || HeroProgressionManager.Instance.Service == null)
            {
                Debug.LogWarning("HeroProgressionRuntimeBridge: HeroProgressionManager not found");
                return;
            }

            EnsureUnlocked();

            var stat = HeroProgressionManager.Instance.Service.GetCurrentStats(heroData.Id);
            if (stat == null)
            {
                Debug.LogWarning($"HeroProgressionRuntimeBridge: stat snapshot null for hero {heroData.Id}");
                return;
            }

            ApplySnapshotToStatsController(stat);
        }

        private void ApplySnapshotToStatsController(HeroStatSnapshot stat)
        {
            var module = statsController.StatModule;
            if (module == null)
            {
                Debug.LogWarning("HeroProgressionRuntimeBridge: StatModule is null");
                return;
            }

            module.SetBaseStat(StatType.MaxHp, stat.Health);
            module.SetBaseStat(StatType.Atk, stat.Attack);
            module.SetBaseStat(StatType.Def, stat.Defense);
            module.SetBaseStat(StatType.Accuracy, stat.Accuracy);
            module.SetBaseStat(StatType.AttackSpeed, stat.AttackSpeed);
            module.SetBaseStat(StatType.AttackRange, stat.AttackRange);
            module.SetBaseStat(StatType.MoveSpeed, stat.MoveSpeed);
            module.SetBaseStat(StatType.CritChance, stat.CritChance);
            module.SetBaseStat(StatType.CritDamage, stat.CritDamage);
        }

        // ===== Debug helpers =====

        public void AddShardDebug(int amount)
        {
            if (heroData == null || HeroProgressionManager.Instance == null) return;

            EnsureUnlocked();
            HeroProgressionManager.Instance.AddShard(heroData.Id, amount);
        }

        public void TryUpgradeDebug()
        {
            if (heroData == null || HeroProgressionManager.Instance == null) return;

            EnsureUnlocked();

            if (HeroProgressionManager.Instance.UpgradeHero(heroData.Id))
            {
                RefreshFromProgression();
            }
        }

        public string GetDebugInfo()
        {
            if (heroData == null || HeroProgressionManager.Instance == null || HeroProgressionManager.Instance.Service == null)
                return "Missing HeroData or HeroProgressionManager";

            EnsureUnlocked();

            var service = HeroProgressionManager.Instance.Service;
            var owned = service.GetOrCreateOwnedHero(heroData.Id);
            var currentNode = service.GetCurrentNode(heroData.Id);
            int maxStar = service.GetMaxStarInCurrentTier(heroData.Id);

            if (owned == null || currentNode == null)
                return "Owned data or current node is null";

            return $"Hero: {heroData.Name} ({heroData.Id}) | Tier: {owned.CurrentTier} | Star: {owned.CurrentStarInTier}/{maxStar} | Shard: {owned.CurrentShard} | CostToNext: {(currentNode.IsMaxNode ? 0 : currentNode.ShardCostToNext)}";
        }
    }
}