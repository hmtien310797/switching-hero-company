using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Hero;

namespace Battle
{
    public partial class PvEBattleController
    {
        public event Action OnActiveLineupChanged;

        //lấy list Hero
        public IReadOnlyList<PlayerHeroController> GetActiveHeroControllers()
        {
            //implement then
            return null;
        }

        //lấy hero theo class
        public PlayerHeroController TryGetActiveHeroByClass(HeroClass heroClass)
        {
            //implement then
            // for (int i = 0; i < inBattleHeroCollection.Length; i++)
            // {
            //     PlayerHeroController currentHero = inBattleHeroCollection[i];
            //     if (currentHero.HeroClass == heroClass)
            //     {
            //         return currentHero;
            //     }
            // }
            //
            return null;
        }

        public bool HasActiveHeroOfClass(HeroClass heroClass)
        {
            return TryGetActiveHeroByClass(heroClass);
        }
        
        private void NotifyActiveLineupChanged()
        {
            OnActiveLineupChanged?.Invoke();
        }
    }
}