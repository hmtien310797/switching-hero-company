using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Shared.Database;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared
{
    public class DatabaseManager : Singleton<DatabaseManager>
    {
        [Header("Database config")]
        [field: SerializeField]
        public EquipmentTierDatabaseSO EquipmentTierDatabase { get; private set; }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}