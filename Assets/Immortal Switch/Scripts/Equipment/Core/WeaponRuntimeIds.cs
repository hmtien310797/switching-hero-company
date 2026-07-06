namespace Immortal_Switch.Scripts.Equipment.Core
{
    public static class WeaponRuntimeIds
    {
        public static string HeroPrefix(int heroId)
            => $"EQUIPMENT:HERO_{heroId}:";
        public static string Standard(int heroId, int weaponId)
            => $"{HeroPrefix(heroId)} STANDARD: {weaponId}";

        public static string Exclusive(int heroId, int weaponId)
            => $"{HeroPrefix(heroId)} EXCLUSIVE: {weaponId}";
    }
}