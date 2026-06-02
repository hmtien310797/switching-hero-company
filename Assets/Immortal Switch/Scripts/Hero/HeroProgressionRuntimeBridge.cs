using Battle;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Hero
{
    public class HeroProgressionRuntimeBridge : MonoBehaviour
    {
        private int registeredHeroId = -1;

        private HeroDataSO heroData;
        private HeroActor heroActor;
        private StatsController statsController;

        private void Awake()
        {
            if (statsController == null)
            {
                if (heroActor != null)
                    statsController = heroActor.Stats;
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

        public void Setup(HeroDataSO data, HeroActor heroActor)
        {
            TryUnregister();

            heroData = data;
            this.heroActor = heroActor;
            statsController = heroActor.Stats;

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