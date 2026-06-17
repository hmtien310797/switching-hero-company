using System.Collections.Generic;
using Common;
using UnityEngine;

namespace Immortal_Switch.Scripts.Hero
{
    [CreateAssetMenu(fileName = "HeroProgressionDatabase", menuName = "ScriptableObjects/Heroes/HeroProgressionDatabase")]
    public class HeroProgressionDatabaseSO : ScriptableObject
    {
        public List<HeroProgressionConfigSO> ProgressionConfigs = new();

        public HeroDataSO GetHero(int heroId)
        {
            return MasterDataCache.Instance.GetHeroDataById(heroId);
        }

        public HeroProgressionConfigSO GetProgressionConfig(int heroId)
        {
            return ProgressionConfigs.Find(x => x != null && x.HeroId == heroId);
        }
    }
}