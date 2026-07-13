using Immortal_Switch.Scripts.TransmutationSystem.Models;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [field: DatabaseBinding]
        public TransmutationSystemDatabaseSO TransmutationSystemDatabase { get; private set; }
    }
}