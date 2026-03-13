using Immortal_Switch.Scripts;
using System.Collections.Generic;
using UnityEngine;

public class MasterDataCache : MonoBehaviour
{
    public static MasterDataCache Instance;

    [SerializeField] List<HeroDataSO> heroDatas;

    private Dictionary<int,HeroDataSO> heroDataDicts = new Dictionary<int, HeroDataSO> ();

    void Start()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitHeroData();
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

}
