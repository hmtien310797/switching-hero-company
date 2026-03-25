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

        private int registeredHeroId = -1;

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

        private void OnEnable()
        {
            TryRegister();
        }

        private void OnDisable()
        {
            TryUnregister();
        }

        public void Setup(HeroDataSO data, PlayerHeroController controller = null)
        {
            TryUnregister();

            heroData = data;

            if (controller != null)
                playerHeroController = controller;

            if (playerHeroController != null && statsController == null)
                statsController = playerHeroController.Stats;

            TryRegister();
        }

        private void TryRegister()
        {
            if (heroData == null) return;
            if (HeroProgressionManager.Instance == null) return;

            registeredHeroId = heroData.Id;
            HeroProgressionManager.Instance.RegisterBridge(registeredHeroId, this);
        }

        private void TryUnregister()
        {
            if (registeredHeroId < 0) return;
            if (HeroProgressionManager.Instance == null) return;

            HeroProgressionManager.Instance.UnregisterBridge(registeredHeroId, this);
            registeredHeroId = -1;
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
    }
}