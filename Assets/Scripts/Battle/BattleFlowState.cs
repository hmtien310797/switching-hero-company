namespace Immortal_Switch.Scripts.Battle
{
    public enum BattleFlowState
    {
        None = 0,

        ChapterRunning = 1,

        EnteringDungeon = 2,
        DungeonRunning = 3,
        DungeonResult = 4,

        ReturningToChapter = 5
    }
}