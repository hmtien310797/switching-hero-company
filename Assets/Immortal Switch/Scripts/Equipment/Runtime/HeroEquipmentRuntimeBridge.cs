using Battle;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.Services;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.Runtime
{
    public class HeroEquipmentRuntimeBridge : MonoBehaviour
    {
        [SerializeField] private PlayerHeroController playerHeroController;
        [SerializeField] private StatsController statsController;

        private int heroId = -1;
        private HeroClass heroClass;

        public int HeroId => heroId;

        private void Awake()
        {
            if (playerHeroController == null)
                playerHeroController = GetComponent<PlayerHeroController>();

            if (statsController == null && playerHeroController != null)
                statsController = playerHeroController.Stats;
        }

        private void OnEnable()
        {
            TryCacheHeroInfo();

            if (heroId > 0)
                HeroEquipmentRuntimeRegistry.Register(heroId, this);
        }

        private void OnDisable()
        {
            if (heroId > 0)
                HeroEquipmentRuntimeRegistry.Unregister(heroId, this);
        }

        public void Setup(PlayerHeroController controller)
        {
            playerHeroController = controller;
            statsController = controller != null ? controller.Stats : null;
            TryCacheHeroInfo();

            if (heroId > 0)
                HeroEquipmentRuntimeRegistry.Register(heroId, this);
        }

        private void TryCacheHeroInfo()
        {
            if (playerHeroController == null)
                return;

            heroId = playerHeroController.GetHeroId();
            heroClass = playerHeroController.HeroClass;
        }

        public void RefreshFromEquipment()
        {
            if (WeaponManager.Instance == null)
                return;

            if (statsController == null || statsController.StatModule == null)
                return;

            TryCacheHeroInfo();

            var module = statsController.StatModule;
            
            RemoveCurrentWeaponSources(module);

            var inventory = WeaponManager.Instance.Inventory;
            var activeSource = inventory.ResolveActiveSource(heroId);

            switch (activeSource)
            {
                case WeaponEquipSource.Standard:
                    ApplyStandard(module);
                    break;

                case WeaponEquipSource.Exclusive:
                    ApplyExclusive(module);
                    break;
            }
        }

        public bool IsUsingStandardWeapon(int weaponId)
        {
            if (WeaponManager.Instance == null || heroId <= 0)
                return false;

            var equip = WeaponManager.Instance.Inventory.GetOrCreateHeroEquip(heroId);
            return equip.EquippedStandardWeaponId == weaponId;
        }

        private void RemoveCurrentWeaponSources(StatModule module)
        {
            module.RemoveModifiersBySourcePrefix(WeaponRuntimeIds.HeroPrefix(heroId));
        }

        private void ApplyStandard(StatModule module)
        {
            var inventory = WeaponManager.Instance.Inventory;
            var equip = inventory.GetOrCreateHeroEquip(heroId);

            if (equip.EquippedStandardWeaponId <= 0)
                return;

            var def = WeaponManager.Instance.Database.GetStandard(equip.EquippedStandardWeaponId);
            if (def == null || def.WeaponClass != heroClass)
                return;

            var state = inventory.GetOrCreateStandardState(def.WeaponId);
            if (!state.IsUnlocked)
                return;

            string sourceId = WeaponRuntimeIds.Standard(heroId, def.WeaponId);
            var modifiers = WeaponStatBuilder.BuildForStandard(def, state, sourceId);

            for (int i = 0; i < modifiers.Count; i++)
                module.AddModifier(modifiers[i]);
        }

        private void ApplyExclusive(StatModule module)
        {
            var inventory = WeaponManager.Instance.Inventory;
            var equip = inventory.GetOrCreateHeroEquip(heroId);

            if (equip.EquippedExclusiveWeaponId <= 0)
                return;

            var def = WeaponManager.Instance.Database.GetExclusive(equip.EquippedExclusiveWeaponId);
            if (def == null || def.HeroId != heroId)
                return;

            var state = inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            if (!state.IsUnlocked)
                return;

            string sourceId = WeaponRuntimeIds.Exclusive(heroId, def.ExclusiveWeaponId);
            var modifiers = WeaponStatBuilder.BuildForExclusive(def, state, sourceId);

            for (int i = 0; i < modifiers.Count; i++)
                module.AddModifier(modifiers[i]);
        }
    }
}