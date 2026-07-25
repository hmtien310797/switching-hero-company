namespace Immortal_Switch.Scripts.Core
{
    public static class GameEvents
    {
        public const string OnPlayerDead = "OnPlayerDead";
        public const string OnEnemyDead = "OnEnemyDead";
        public const string OnGoldChanged = "OnGoldChanged";
        public const string OnWaveStart = "OnWaveStart";
        public const string OnWaveEnd = "OnStageStart";
        public const string OnStageCleared = "OnStageCleared";
        public const string OnStageLost = "OnStageLost";
        public const string OnBossSpawnAnimationComplete = "OnBossSpawnAnimationComplete";
        public const string OnBossDead = "OnBossDead";

        public const string OnInitNewStage = "OnInitNewStage";

        // Change hero event
        public const string OnChangeHero = "OnChangeHero";

        // Change skill event
        public const string OnChangeSkill = "OnChangeSkill";
        public const string OnSpawnNextStage = "OnSpawnNextStage";
        public const string OnMoveStageRequested = "OnMoveStageRequested";
        public const string OnToggleMainView = "OnToggleMainView";
        public const string OnUserLogOut = "OnUserLogOut";
        public const string OnAppPaused = "OnAppPaused";
        public const string OnAppResumed = "OnAppResumed";
        public const string OnAppQuit = "OnAppQuit";
        public const string OnActiveLineupChanged = "OnActiveLineupChanged";
        public const string OnInitSceneDataComplete = "OnInitSceneDataComplete";
        public const string OnStageSessionChange = "OnStageSessionChange";
        public const string OnStageSessionEnd = "OnStageSessionEnd";
        public const string OnPlayDungeon = "OnPlayDungeon";

        public const string OnKillAllDungeonInit = "OnKillAllDungeonInit";
        public const string OnKillAllDungeonEnemyCountChanged = "OnKillAllDungeonInit";

        public const string OnDefenseDungeonInit = "OnDefenseDungeonInit";
        public const string OnDefenseDungeonDataChanged = "OnDefenseDungeonDataChanged";

        public const string OnSelectedDungeonStage = "OnSelectedDungeonStage";

        // summon
        public const string ON_SUMMON_HERO = nameof(ON_SUMMON_HERO);

        // equipment
        public const string ON_ENHANCE_GEAR = nameof(ON_ENHANCE_GEAR);
        public const string ON_EQUIP_ITEM = nameof(ON_EQUIP_ITEM);

        // progression
        public const string ON_HERO_LEVEL_UP = nameof(ON_HERO_LEVEL_UP);
        public const string ON_SKILL_UPGRADE = nameof(ON_SKILL_UPGRADE);

        // afk reward
        public const string ON_AFK_REWARD_CLAIM_COUNT = nameof(ON_AFK_REWARD_CLAIM_COUNT);

        // dungeon
        public const string ON_DUNGEON_CLEAR = nameof(ON_DUNGEON_CLEAR);
    }
}
