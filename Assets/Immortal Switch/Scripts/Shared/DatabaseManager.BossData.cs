using System.Collections.Generic;
using Immortal_Switch.Scripts.Boss;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [SerializeField] List<BossDataSO> bossData;
        
        private Dictionary<int,BossDataSO> bossDataMapper = new Dictionary<int, BossDataSO> ();

        private void InitBossData()
        {
            bossDataMapper.Clear();
            foreach (var bossData in bossData)
            {
                bossDataMapper[bossData.Id] = bossData;
            }
        }
        
        public bool TryGetBossData(int enemyId, out BossDataSO bossData)
        {
            bossData = null;

            if (bossDataMapper == null || bossDataMapper.Count == 0) return false;
            if (!bossDataMapper.TryGetValue(enemyId, out bossData)) return false;

            return bossData != null;
        }
    }
}