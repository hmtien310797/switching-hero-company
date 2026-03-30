using System;
using System.Collections.Generic;
using Immortal_Switch.Hero;

namespace Scripts.Battle
{
    public partial class PvEBattleController
    {
        public event Action OnActiveLineupChanged;

        public IReadOnlyList<PlayerHeroController> GetActiveHeroControllers()
        {
            var result = new List<PlayerHeroController>(2);

            if (firstPlayerHeroController != null && firstPlayerHeroController.gameObject.activeInHierarchy)
                result.Add(firstPlayerHeroController);

            if (secondPlayerHeroController != null && secondPlayerHeroController.gameObject.activeInHierarchy)
                result.Add(secondPlayerHeroController);

            return result;
        }

        public bool TryGetActiveHeroByClass(HeroClass heroClass, out PlayerHeroController hero)
        {
            hero = null;

            if (firstPlayerHeroController != null &&
                firstPlayerHeroController.gameObject.activeInHierarchy &&
                firstPlayerHeroController.HeroClass == heroClass)
            {
                hero = firstPlayerHeroController;
                return true;
            }

            if (secondPlayerHeroController != null &&
                secondPlayerHeroController.gameObject.activeInHierarchy &&
                secondPlayerHeroController.HeroClass == heroClass)
            {
                hero = secondPlayerHeroController;
                return true;
            }

            return false;
        }

        public bool HasActiveHeroOfClass(HeroClass heroClass)
        {
            return TryGetActiveHeroByClass(heroClass, out _);
        }

        public PlayerHeroController GetHeroInSlot(int slotIndex)
        {
            return slotIndex switch
            {
                0 => firstPlayerHeroController,
                1 => secondPlayerHeroController,
                _ => null
            };
        }

        private void NotifyActiveLineupChanged()
        {
            OnActiveLineupChanged?.Invoke();
        }
    }
}