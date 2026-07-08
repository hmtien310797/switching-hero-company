using System.Linq;

namespace Common
{
    public partial class UserDataCache
    {
#region HERO

        public bool AreAllBattleHeroesSameClass()
        {
            if (inBattleHeroes.Length <= 1)
            {
                return false;
            }

            var heroClass = inBattleHeroes[0].HeroClass;
            return inBattleHeroes.All(hero => hero.HeroClass == heroClass);
        }

#endregion
    }
}