using Immortal_Switch.Scripts;
using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Hero;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;


namespace Scripts.Common
{
    public class HeroDataOwn
    {
        public List<HeroDataSO> OwnedHeros;
    }

    public class SkillDataOwn
    {
        public Dictionary<int, SkillDataSO> OwnedSkills;
    }

    public class SelectedHero
    {
        public int MainHeroId;
        public int SubHeroId;
    }

    public class SelectedHeroSkill
    {
        public Dictionary<int, List<SkillDataSO>> SelectedSkillData;
    }

    public class UserDataCache : MonoBehaviour
    {
        public static UserDataCache Instance;

        public HeroDataOwn OwnedHeroData;
        public SelectedHero SelectedHeros;
        public SkillDataOwn OwnedSkillData;
        public SelectedHeroSkill SelectedHeroSkills;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            SetSelectedHeros(1, 3);
            SetOwnedHeros(new List<int>(){ 1,2,3});
            SetOwnedSkills(MasterDataCache.Instance.GetAllSkills());
            SetSelectedSkillByHeroId(1, new List<SkillDataSO>() 
            { 
                OwnedSkillData.OwnedSkills[201], OwnedSkillData.OwnedSkills[211], OwnedSkillData.OwnedSkills[212], OwnedSkillData.OwnedSkills[221], OwnedSkillData.OwnedSkills[222] 
            });
            SetSelectedSkillByHeroId(2, new List<SkillDataSO>()
            {
                OwnedSkillData.OwnedSkills[101], OwnedSkillData.OwnedSkills[111], OwnedSkillData.OwnedSkills[121], OwnedSkillData.OwnedSkills[122], OwnedSkillData.OwnedSkills[102]
            });
            SetSelectedSkillByHeroId(3, new List<SkillDataSO>()
            {
                OwnedSkillData.OwnedSkills[102], OwnedSkillData.OwnedSkills[122], OwnedSkillData.OwnedSkills[111], OwnedSkillData.OwnedSkills[121], OwnedSkillData.OwnedSkills[101]
            });
        }

        private void SetOwnedHeros(List<int> hids)
        {
            OwnedHeroData = new HeroDataOwn();
            if (OwnedHeroData.OwnedHeros == null)
                OwnedHeroData.OwnedHeros = new List<HeroDataSO>();

            foreach (int hid in hids)
            {
                OwnedHeroData.OwnedHeros.Add(MasterDataCache.Instance.GetHeroDataById(hid));
            }
        }

        public void SetSelectedHeros(int mhid, int shid)
        {
            SelectedHeros = new SelectedHero();
            SelectedHeros.MainHeroId = mhid;
            SelectedHeros.SubHeroId = shid;
        }

        public void SetOwnedSkills(Dictionary<int,SkillDataSO> skills)
        {
            OwnedSkillData = new SkillDataOwn();
            OwnedSkillData.OwnedSkills = skills;
        }

        public void SetSelectedSkillByHeroId(int hid, List<SkillDataSO> skills)
        {
            if(SelectedHeroSkills == null) SelectedHeroSkills = new ();
            if (SelectedHeroSkills.SelectedSkillData == null) SelectedHeroSkills.SelectedSkillData = new();

            SelectedHeroSkills.SelectedSkillData[hid] = skills;
        }

        public List<SkillDataSO> GetSelectedSkillByHeroId(int hid)
        {
            return SelectedHeroSkills.SelectedSkillData[hid];
        }

        public List<int> GetSelectedSkillIdsByHeroId(int hid)
        {
            return SelectedHeroSkills.SelectedSkillData[hid].Select(x => x.SkillId).ToList();
        }
    }
}
