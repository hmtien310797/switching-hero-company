using System.Collections.Generic;
using Immortal_Switch.Scripts.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [SerializeField] List<HeroDataSO> heroData;
        
        private Dictionary<int,HeroDataSO> heroDataDict = new Dictionary<int, HeroDataSO> ();
        
        private void InitHeroData()
        {
            heroDataDict.Clear ();
            foreach (var heroData in heroData)
            {
                heroDataDict[heroData.Id] = heroData;
            }
        }
        
        public HeroDataSO GetHeroDataById(int id)
        {
            if(!heroDataDict.ContainsKey(id)) return null;

            return heroDataDict[id];
        }

        public List<HeroDataSO> GetAllHeroData()
        {
            return heroData;
        }
    }
    
}
