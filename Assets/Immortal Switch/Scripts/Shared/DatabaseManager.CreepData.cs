using System.Collections.Generic;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [SerializeField] List<CreepDataSo> creepData;
        
        private Dictionary<int,CreepDataSo> creepDataMapper = new Dictionary<int, CreepDataSo> ();

        public bool TryGetCreepData(int enemyId, out CreepDataSo creepData)
        {
            creepData = null;

            if (creepDataMapper == null || creepDataMapper.Count == 0) return false;
            if (!creepDataMapper.TryGetValue(enemyId, out creepData)) return false;

            return creepData != null;
        }
        
        private void InitCreepData()
        {
            creepDataMapper.Clear ();
            foreach (var creepData in creepData)
            {
                creepDataMapper[creepData.Id] = creepData;
            }
        }
    }
}