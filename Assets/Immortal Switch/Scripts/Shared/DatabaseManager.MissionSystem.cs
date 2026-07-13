using Immortal_Switch.Scripts.MissionSystem.Models;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [field: DatabaseBinding]
        public MissionSystemDatabaseSO MissionSystemDatabase {get; private set;}
    }
}