using System.Collections.Generic;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;
using UnityEngine.Serialization;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [FormerlySerializedAs("skillData")] 
        [SerializeField] List<SkillDataSO> classSkillData;
        [SerializeField] List<SkillDataSO> ultimateSkillData;
        [SerializeField] List<SkillDataSO> passiveSkillData;
        
        private Dictionary<int,SkillDataSO> skillDataDict = new Dictionary<int, SkillDataSO> ();
        
        private void InitSkillData()
        {
            skillDataDict.Clear();
            for (int i = 0; i < classSkillData.Count; i++)
            {
                SkillDataSO currentSkillData = classSkillData[i];
                skillDataDict.Add(currentSkillData.SkillId, currentSkillData);
            }
        }
        
        public SkillDataSO GetSkillDataById(int id)
        {
            return skillDataDict.GetValueOrDefault(id);
        }

        public List<SkillDataSO> GetAllSkillData()
        {
            return classSkillData;
        }

        public SkillDataSO GetUltimateSkillDataByHeroId(int heroId)
        {
            return ultimateSkillData.Find(x => x.HeroId == heroId);
        }

        public SkillDataSO GetPassiveSkillDataByHeroId(int heroId)
        {
            return passiveSkillData.Find(x => x.HeroId == heroId);
        }
    }
}