using System;

namespace Battle.Dungeon
{
    public static class DungeonModeFactory
    {
        public static IDungeonMode Create(DungeonModeType modeType)
        {
            return modeType switch
            {
                DungeonModeType.KillAllEnemies => new DungeonKillAllMode(),
                DungeonModeType.DefendObjective => new DungeonDefenseMode(),
                DungeonModeType.DamageChallenge => new DungeonDamageChallengeMode(),
                DungeonModeType.BossChallenge => new DungeonBossChallengeMode(),
                _ => throw new ArgumentOutOfRangeException(nameof(modeType), modeType, null)
            };
        }
    }
}
