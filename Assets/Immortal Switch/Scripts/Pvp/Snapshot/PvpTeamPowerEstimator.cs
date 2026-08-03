using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    /// <summary>
    /// Ước lượng team power từ FinalStats (proxy CP). <b>FLAGGED:</b> rough — production nên dùng
    /// PowerService.CalculateHeroCp (cần live StatsController + player level). Dùng cho cả player
    /// team (snapshot FinalStats) và mock team.
    /// </summary>
    public static class PvpTeamPowerEstimator
    {
        public static long Estimate(RuntimeStatSnapshot s)
        {
            if (s == null) return 0;
            long hp = (long)s.Get(StatType.MaxHp);
            long atk = (long)s.Get(StatType.Atk);
            long def = (long)s.Get(StatType.Def);
            return hp + atk * 14 + def * 6;
        }

        public static long EstimateTeam(RuntimeStatSnapshot a, RuntimeStatSnapshot b)
            => Estimate(a) + Estimate(b);
    }
}
