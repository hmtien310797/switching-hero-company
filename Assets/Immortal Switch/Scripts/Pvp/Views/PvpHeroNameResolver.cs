using Common;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Resolve hero id → display name từ <see cref="UserDataCache"/> (server-owned list). Không gọi ES3.
    /// Fallback "Hero {id}" nếu chưa có trong cache (hero vừa summon, v.v.).
    /// </summary>
    internal static class PvpHeroNameResolver
    {
        public static string Get(int heroId)
        {
            if (heroId <= 0) return "—";

            try
            {
                var owned = UserDataCache.Instance?.HeroList?.Owned;
                if (owned == null) return $"Hero {heroId}";

                foreach (var h in owned)
                {
                    if (h != null && h.HeroId == heroId)
                        return string.IsNullOrEmpty(h.Name) ? $"Hero {heroId}" : h.Name;
                }
            }
            catch
            {
                // ignore — fallback
            }

            return $"Hero {heroId}";
        }
    }
}
