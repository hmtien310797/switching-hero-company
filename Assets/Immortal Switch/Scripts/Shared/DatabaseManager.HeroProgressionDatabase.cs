using Immortal_Switch.Scripts.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [field: DatabaseBinding]
        public HeroProgressionDatabaseSO HeroProgressionDatabase { get; private set; }
    }
}