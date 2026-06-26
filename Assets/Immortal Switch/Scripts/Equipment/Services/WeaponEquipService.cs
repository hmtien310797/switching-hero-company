using Immortal_Switch.Scripts.Equipment.Definitions;
using Immortal_Switch.Scripts.Hero;

namespace Immortal_Switch.Scripts.Equipment.Services
{
    public class WeaponEquipService
    {
        private readonly WeaponDatabaseSO database;
        private readonly WeaponInventoryService inventory;

        public WeaponEquipService(WeaponDatabaseSO database, WeaponInventoryService inventory)
        {
            this.database = database;
            this.inventory = inventory;
        }

        public bool EquipStandard(int heroId, HeroClass heroClass, int weaponId)
        {
            var def = database.GetStandard(weaponId);
            if (def == null)
                return false;

            if (def.WeaponClass != heroClass)
                return false;

            var state = inventory.GetOrCreateStandardState(weaponId);
            if (!state.IsUnlocked)
                return false;

            var equip = inventory.GetOrCreateHeroEquip(heroId);

            // Replace standard cũ bằng standard mới
            equip.EquippedStandardWeaponId = weaponId;

            // Không động vào exclusive id ở đây
            // vì active source sẽ resolve theo UseExclusive
            return true;
        }

        public bool EquipExclusive(int heroId)
        {
            var def = database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return false;

            var state = inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            if (!state.IsUnlocked)
                return false;

            var equip = inventory.GetOrCreateHeroEquip(heroId);
            equip.EquippedExclusiveWeaponId = def.ExclusiveWeaponId;
            equip.UseExclusive = true;

            return true;
        }

        public void SetUseExclusive(int heroId, bool useExclusive)
        {
            var equip = inventory.GetOrCreateHeroEquip(heroId);
            equip.UseExclusive = useExclusive;
        }

        public void ClearStandard(int heroId)
        {
            var equip = inventory.GetOrCreateHeroEquip(heroId);
            equip.EquippedStandardWeaponId = 0;
        }

        public void ClearExclusive(int heroId)
        {
            var equip = inventory.GetOrCreateHeroEquip(heroId);
            equip.EquippedExclusiveWeaponId = 0;
            equip.UseExclusive = false;
        }
    }
}