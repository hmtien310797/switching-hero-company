using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Hero;

namespace Battle
{
    public partial class PvEBattleController
    {
        public event Action OnActiveLineupChanged;

        public IReadOnlyList<PlayerHeroController> GetActiveHeroControllers()
        {
            return inBattleHeroCollection;
        }

        public PlayerHeroController TryGetActiveHeroByClass(HeroClass heroClass)
        {
            for (int i = 0; i < inBattleHeroCollection.Length; i++)
            {
                PlayerHeroController currentHero = inBattleHeroCollection[i];
                if (currentHero.HeroClass == heroClass)
                {
                    return currentHero;
                }
            }

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