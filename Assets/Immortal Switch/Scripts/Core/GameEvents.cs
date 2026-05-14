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
        public const string OnBossDead = "OnBossDead";
        // Change hero event
        public const string OnChangeHero = "OnChangeHero";
        // Change skill event
        public const string OnChangeSkill = "OnChangeSkill";
        public const string OnNextStageButtonClicked = "OnNextStageButtonClicked";
    }
}