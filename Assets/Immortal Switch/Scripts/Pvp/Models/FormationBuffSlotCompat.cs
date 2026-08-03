namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Khả năng trang bị buff theo vị trí (DOCX §17 — "FrontOnly/BackOnly compatibility").
    /// Any = được equip ở Front hoặc Back.
    /// </summary>
    public enum FormationBuffSlotCompat
    {
        Any = 0,
        FrontOnly = 1,
        BackOnly = 2
    }
}
