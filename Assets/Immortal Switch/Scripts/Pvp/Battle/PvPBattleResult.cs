namespace Immortal_Switch.Scripts.Pvp.Battle
{
    /// <summary>Kết quả battle (DOCX §15, §34). Surrender = rời sau BattleId (§2 Exit rule).</summary>
    public enum PvPBattleResult
    {
        None = 0,
        Victory = 1,
        Defeat = 2,
        Draw = 3,
        Surrender = 4
    }
}
