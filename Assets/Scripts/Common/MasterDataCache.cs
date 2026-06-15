using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;

namespace Common
{
    public class MasterDataCache : Singleton<MasterDataCache>
    {
        [SerializeField] List<HeroDataSO> heroDatas;
        [SerializeField] List<CreepDataSo> creepDatas;
        [SerializeField] List<BossDataSO> bossDatas;
        [SerializeField] List<SkillDataSO> skillDatas;

        private Dictionary<int,HeroDataSO> heroDataDicts = new Dictionary<int, HeroDataSO> ();
        private Dictionary<int,SkillDataSO> skillDataDicts = new Dictionary<int, SkillDataSO> ();
        private Dictionary<int,CreepDataSo> creepDataMapper = new Dictionary<int, CreepDataSo> ();
        private Dictionary<int,BossDataSO> bossDataMapper = new Dictionary<int, BossDataSO> ();
    
        public override UniTask InitializeAsync()
        {
            InitHeroData();
            InitSkillData();
            InitCreepData();
            InitBossData();
            return UniTask.CompletedTask;
        }
        
        public bool TryGetCreepData(int enemyId, out CreepDataSo creepData)
        {
            creepData = null;

            if (creepDataMapper == null || creepDataMapper.Count == 0) return false;
            if (!creepDataMapper.TryGetValue(enemyId, out creepData)) return false;
            
            return creepData != null;
        }
        
        public bool TryGetBossData(int enemyId, out BossDataSO bossData)
        {
            bossData = null;

            if (bossDataMapper == null || bossDataMapper.Count == 0) return false;
            if (!bossDataMapper.TryGetValue(enemyId, out bossData)) return false;
            
            return bossData != null;
        }

        #region Hero

        public HeroDataSO GetHeroDataById(int id)
        {
            if(!heroDataDicts.ContainsKey(id)) return null;

            return heroDataDicts[id];
        }

        public List<HeroDataSO> GetAllHeroData()
        {
            return heroDatas;
        }

        #endregion

        public SkillDataSO GetSkillDataById(int id)
        {
            if (!skillDataDicts.ContainsKey(id)) return null;
            return skillDataDicts[id];
        }
        
        #region Init Data
        private void InitSkillData()
        {
            skillDataDicts.Clear();
        }
        
        private void InitHeroData()
        {
            heroDataDicts.Clear ();
            foreach (var heroData in heroDatas)
            {
                heroDataDicts[heroData.Id] = heroData;
            }
        }
        
        private void InitCreepData()
        {
            creepDataMapper.Clear ();
            foreach (var creepData in creepDatas)
            {
                creepDataMapper[creepData.Id] = creepData;
            }
        }
        
        private void InitBossData()
        {
            bossDataMapper.Clear ();
            foreach (var bossData in bossDatas)
            {
                bossDataMapper[bossData.Id] = bossData;
            }
        }
        #endregion
    }
}
