namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Các bậc rank PvP. DOCX không liệt kê bậc cụ thể — lấy từ Markdown (screen 12:
    /// Bronze → Silver → Gold → Platinum) + wireframe. Phase-1 là simulation local;
    /// rank/reward config tách riêng để server thay sau (DOCX §32).
    /// </summary>
    public enum PvpRankTier
    {
        Bronze = 0,
        Silver = 1,
        Gold = 2,
        Platinum = 3,
        Diamond = 4
    }
}
