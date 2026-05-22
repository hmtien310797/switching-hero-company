using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;

namespace Common
{
    public class MasterDataCache : Singleton<MasterDataCache>
    {
        [SerializeField] List<HeroDataSO> heroDatas;
        [SerializeField] List<SkillDataSO> skillDatas;

        private Dictionary<int,HeroDataSO> heroDataDicts = new Dictionary<int, HeroDataSO> ();
        private Dictionary<int,SkillDataSO> skillDataDicts = new Dictionary<int, SkillDataSO> ();
    
        public override UniTask InitializeAsync()
        {
            InitHeroData();
            InitSkillData();
            return UniTask.CompletedTask;
        }

        private void InitHeroData()
        {
            heroDataDicts.Clear ();
            foreach (var heroData in heroDatas)
            {
                heroDataDicts[heroData.Id] = heroData;
            }
        }

        public HeroDataSO GetHeroDataById(int id)
        {
            if(!heroDataDicts.ContainsKey(id)) return null;

            return heroDataDicts[id];
        }

        private void InitSkillData()
        {
            skillDataDicts.Clear();
            //remake later
            // foreach (var heroData in skillDatas)
            // {
            //     skillDataDicts[heroData.SkillId] = heroData;
            // }
        }

        public SkillDataSO GetSkillDataById(int id)
        {
            if (!skillDataDicts.ContainsKey(id)) return null;

            return skillDataDicts[id];
        }

        public Dictionary<int,SkillDataSO> GetAllSkills()
        {
            return skillDataDicts;
        }

    }
}
