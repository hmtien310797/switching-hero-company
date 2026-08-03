using System;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    /// <summary>Team 2 hero (Front/Back) + team power (DOCX §10).</summary>
    [Serializable]
    public sealed class TeamBattleSnapshot
    {
        public HeroBattleSnapshot FrontHero;
        public HeroBattleSnapshot BackHero;
        public long TeamPower;
    }
}
