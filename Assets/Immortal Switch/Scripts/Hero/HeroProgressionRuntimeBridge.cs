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
            var stat = HeroProgressionManager.Instance.Service.GetCurrentStats(heroData.Id);
            if (stat == null)
            {
                Debug.LogError($"HeroProgressionRuntimeBridge: stat snapshot null for hero {heroData.Id}");
                return;
            }

            ApplySnapshotToStatsController(stat);
        }

        private void ApplySnapshotToStatsController(HeroStatSnapshot stat)
        {
            var module = statsController.StatModule;
            if (module == null)
            {
                Debug.LogError("HeroProgressionRuntimeBridge: StatModule is null");
                return;
            }

            module.SetBaseStat(StatType.MaxHp, stat.Health);
            module.SetBaseStat(StatType.Atk, stat.Attack);
            module.SetBaseStat(StatType.Def, stat.Defense);
            //temp lock
            // module.SetBaseStat(StatType.Accuracy, stat.Accuracy);
            // module.SetBaseStat(StatType.AttackSpeed, stat.AttackSpeed);
            // module.SetBaseStat(StatType.AttackRange, stat.AttackRange);
            // module.SetBaseStat(StatType.MoveSpeed, stat.MoveSpeed);
            // module.SetBaseStat(StatType.CritChance, stat.CritChance);
            // module.SetBaseStat(StatType.CritDamage, stat.CritDamage);
        }
    }
}