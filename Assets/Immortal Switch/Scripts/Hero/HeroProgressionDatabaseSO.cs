using System.Collections.Generic;
using UnityEngine;

namespace Immortal_Switch.Hero
{
    [CreateAssetMenu(fileName = "HeroProgressionDatabase", menuName = "ScriptableObjects/Heroes/HeroProgressionDatabase")]
    public class HeroProgressionDatabaseSO : ScriptableObject
    {
        public List<HeroDataSO> Heroes = new();
        public List<HeroProgressionConfigSO> ProgressionConfigs = new();

        public HeroDataSO GetHero(int heroId)
        {
            return Heroes.Find(x => x != null && x.Id == heroId);
        }

        public HeroProgressionConfigSO GetProgressionConfig(int heroId)
        {
            return ProgressionConfigs.Find(x => x != null && x.Hero != null && x.Hero.Id == heroId);
        }
    }
}