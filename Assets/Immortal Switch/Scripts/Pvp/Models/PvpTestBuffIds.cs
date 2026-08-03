using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// BuffId test mặc định được cấp ở first-run (DOCX §7 — "Grant a minimal test buff
    /// collection covering Core, Support, and Trigger slots"). Phủ đủ 3 slot type.
    /// SO định nghĩa effect thật ở M2 (<c>FormationBuffDataSO</c>). Tên lấy từ Markdown/wireframe.
    /// </summary>
    public static class PvpTestBuffIds
    {
        public const string IronCore = "pvp_buff_iron_core";        // Core slot — MaxHp
        public const string ArcaneFlow = "pvp_buff_arcane_flow";    // Support slot — CooldownReduction
        public const string LastStand = "pvp_buff_last_stand";      // Trigger slot — Low HP Shield
        public const string GuardianLink = "pvp_buff_guardian_link";// Support slot — FrontOnly

        public static readonly IReadOnlyList<string> All =
            new[] { IronCore, ArcaneFlow, LastStand, GuardianLink };
    }
}
