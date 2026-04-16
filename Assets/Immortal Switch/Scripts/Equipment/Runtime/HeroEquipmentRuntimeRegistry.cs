using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Equipment.Runtime
{
    public static class HeroEquipmentRuntimeRegistry
    {
        private static readonly Dictionary<int, HeroEquipmentRuntimeBridge> bridgesByHeroId = new();

        public static void Register(int heroId, HeroEquipmentRuntimeBridge bridge)
        {
            if (heroId <= 0 || bridge == null)
                return;

            bridgesByHeroId[heroId] = bridge;
        }

        public static void Unregister(int heroId, HeroEquipmentRuntimeBridge bridge)
        {
            if (heroId <= 0 || bridge == null)
                return;

            if (!bridgesByHeroId.TryGetValue(heroId, out var current))
                return;

            if (current != bridge)
                return;

            bridgesByHeroId.Remove(heroId);
        }

        public static void RefreshHero(int heroId)
        {
            if (heroId <= 0)
                return;

            if (bridgesByHeroId.TryGetValue(heroId, out var bridge) && bridge != null)
                bridge.RefreshFromEquipment();
        }

        public static void RefreshHeroesUsingStandard(int weaponId)
        {
            foreach (var pair in bridgesByHeroId)
            {
                var bridge = pair.Value;
                if (bridge == null)
                    continue;

                if (bridge.IsUsingStandardWeapon(weaponId))
                    bridge.RefreshFromEquipment();
            }
        }
    }
}