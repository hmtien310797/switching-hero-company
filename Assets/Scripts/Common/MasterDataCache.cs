using Immortal_Switch.Scripts;
using System.Collections.Generic;
using Immortal_Switch.Hero;
using UnityEngine;

public class MasterDataCache : MonoBehaviour
{
    public static MasterDataCache Instance;

    [SerializeField] List<HeroDataSO> heroDatas;
    [SerializeField] List<SkillDataSO> skillDatas;

    private Dictionary<int,HeroDataSO> heroDataDicts = new Dictionary<int, HeroDataSO> ();
    private Dictionary<int,SkillDataSO> skillDataDicts = new Dictionary<int, SkillDataSO> ();

    void Start()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitHeroData();
        InitSkillData();
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
        foreach (var heroData in skillDatas)
        {
            skillDataDicts[heroData.SkillId] = heroData;
        }
    }

    public SkillDataSO GetSkillDataById(int id)
    {
        if (!skillDataDicts.ContainsKey(id)) return null;

        return skillDataDicts[id];
    }

}
