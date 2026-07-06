using System.Collections.Generic;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [SerializeField] List<SkillDataSO> skillData;
        
        private Dictionary<int,SkillDataSO> skillDataDict = new Dictionary<int, SkillDataSO> ();
        
        private void InitSkillData()
        {
            skillDataDict.Clear();
            for (int i = 0; i < skillData.Count; i++)
            {
                SkillDataSO currentSkillData = skillData[i];
                skillDataDict.Add(currentSkillData.SkillId, currentSkillData);
            }
        }
        
        public SkillDataSO GetSkillDataById(int id)
        {
            return skillDataDict.GetValueOrDefault(id);
        }

        public List<SkillDataSO> GetAllSkillData()
        {
            return skillData;
        }
    }
}