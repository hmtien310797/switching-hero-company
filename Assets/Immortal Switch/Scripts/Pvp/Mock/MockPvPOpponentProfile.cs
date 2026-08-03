using System;
using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Mock
{
    /// <summary>
    /// Profile 1 mock opponent (DOCX §30 — "MockPvPOpponentProfile"). Stable seed từ OpponentId;
    /// lower/equal/higher power band quanh player. Chuyển thành cùng snapshot type với player.
    /// </summary>
    [Serializable]
    public sealed class MockPvPOpponentProfile
    {
        public string OpponentId;
        public string DisplayName;
        public int RankPoint;
        public long TeamPower;
        public MockHeroProgressionData FrontHero;
        public MockHeroProgressionData BackHero;
        public PvpFormationSaveData Formation;
        public int DataVersion = 1;
    }
}
