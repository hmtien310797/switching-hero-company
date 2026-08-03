using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Repositories;

namespace Immortal_Switch.Scripts.Pvp.Services
{
    /// <summary>
    /// Phase-1 battle history service (DOCX §13, §38). Capped retention (Markdown §13).
    /// </summary>
    internal sealed class LocalPvpHistoryService : IPvPHistoryService
    {
        private readonly IPvPRepository _repo;

        public LocalPvpHistoryService(IPvPRepository repo)
        {
            _repo = repo;
        }

        public PvpBattleHistoryData LoadHistory()
        {
            return _repo.Load<PvpBattleHistoryData>(PvpEs3Keys.BattleHistory);
        }

        public void SaveHistory(PvpBattleHistoryData data)
        {
            _repo.Save(PvpEs3Keys.BattleHistory, data ?? new PvpBattleHistoryData());
        }
    }
}
