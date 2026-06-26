using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;

namespace Battle
{
    public partial class PvEBattleController
    {
        public HeroActor TryGetActiveHeroByClass(HeroClass heroClass)
        {
            for (int i = 0; i < userDataCache.inBattleHeroes.Length; i++)
            {
                HeroActor hero = userDataCache.inBattleHeroes[i];

                if (hero == null || hero.IsDead)
                    continue;

                if (hero.HeroClass == heroClass)
                    return hero;
            }

            return null;
        }

        public bool HasActiveHeroOfClass(HeroClass heroClass)
        {
            return TryGetActiveHeroByClass(heroClass) != null;
        }

        private void NotifyActiveLineupChanged()
        {
            GameEventManager.Trigger(GameEvents.OnActiveLineupChanged);
        }
    }
}