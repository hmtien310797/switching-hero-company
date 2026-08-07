using Immortal_Switch.Scripts.Pvp.Data;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        /// <summary>
        /// Bảng tier/rank reward PvP. Bind qua [DatabaseBinding] (label addressable "game_database").
        /// Phase-1 import từ Google Sheet (PvpTierRewardGoogleSheetImporterWindow).
        /// </summary>
        [field: DatabaseBinding]
        public PvpTierRewardDatabaseSO PvpTierRewardDatabase { get; private set; }
    }
}