using Immortal_Switch.Scripts.Level.Stage;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [field: DatabaseBinding] 
        public StageDataResolverSO StageDataResolver{get; private set;}
    }
}