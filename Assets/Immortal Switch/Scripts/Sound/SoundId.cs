namespace Immortal_Switch.Scripts.Sound
{
    public enum SoundId
    {
        None = 0,

        // UI
        ButtonClick = 100,
        ButtonBack = 101,
        PopupOpen = 102,
        PopupClose = 103,
        Toast = 104,
        ToggleOn = 105,
        ToggleOff = 106,
        SliderTick = 107,
        Error = 108,
        Confirm = 109,
        Cancel = 110,

        // Reward / Summon
        RewardClaim = 200,
        SummonStart = 201,
        SummonReveal = 202,
        SummonRare = 203,

        // Battle
        EnemyHit = 300,
        HeroHit = 301,
        EnemyDead = 302,
        SkillCast = 303,
        UltimateCast = 304,
        BossWarning = 305,
        StageClear = 306,
        StageFail = 307,

        // Loop
        AuraLoop = 400,
        WarningLoop = 401,
        ChargingLoop = 402,
    }
}
