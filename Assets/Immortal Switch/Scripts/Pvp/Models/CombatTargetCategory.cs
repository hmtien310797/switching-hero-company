namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Phân loại mục tiêu combat. PvPHero LOẠI TRỪ bonus DamageToHeroMonster và
    /// DamageToNormalMonster (DOCX §3 "PvP damage", §11 "DamageToHeroMonster Rule").
    /// Default PvE giữ behavior cũ — đây là layer phân loại thêm, không thay thế ActorType.
    /// </summary>
    public enum CombatTargetCategory
    {
        PvE = 0,
        PvPHero = 1
    }
}
