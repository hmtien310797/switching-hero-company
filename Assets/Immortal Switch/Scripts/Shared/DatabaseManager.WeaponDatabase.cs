using Immortal_Switch.Scripts.Equipment.Definitions;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [DatabaseBinding] 
        private WeaponDatabaseSO weaponDatabase;

        public WeaponDatabaseSO GetWeaponDatabase() => weaponDatabase;
    }
}