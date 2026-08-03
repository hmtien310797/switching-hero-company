namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Vị trí cố định trong đội hình 2v2. Front và Back KHÔNG thể swap trong trận
    /// (DOCX §8 — "Fixed formation"). Khi Front chết, Back có thể reposition nhưng
    /// vẫn giữ AssignedSlot = Back và giữ Back buffs. Không có swap button/API.
    /// </summary>
    public enum FormationSlot
    {
        Front = 0,
        Back = 1
    }
}
