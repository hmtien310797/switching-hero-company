using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Interfaces
{
    /// <summary>
    /// Battle history service (DOCX §13, §38 — IPvPHistoryService). Capped retention (Markdown §13).
    /// </summary>
    public interface IPvPHistoryService
    {
        PvpBattleHistoryData LoadHistory();
        void SaveHistory(PvpBattleHistoryData data);
    }
}
