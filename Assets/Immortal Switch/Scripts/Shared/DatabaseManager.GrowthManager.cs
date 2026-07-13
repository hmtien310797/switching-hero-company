using Immortal_Switch.Scripts.GrowthSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [field: DatabaseBinding]
        public GrowthDatabaseSO GrowthDatabase { get; private set; }
    }
}