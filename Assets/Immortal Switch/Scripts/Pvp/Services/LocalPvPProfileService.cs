using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Repositories;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Services
{
    /// <summary>
    /// Phase-1 profile service (DOCX §5, §6). Load/save <see cref="PvpPlayerData"/> qua repository;
    /// giữ cache in-memory cho UI đọc sync. Ticket/token mutate + save ngay (transaction-like).
    /// Rank reward application (rank change sau battle) ở M5/M7.
    /// </summary>
    internal sealed class LocalPvPProfileService : IPvPProfileService
    {
        private readonly IPvPRepository _repo;
        private PvpPlayerData _current;

        public LocalPvPProfileService(IPvPRepository repo)
        {
            _repo = repo;
        }

        public UniTask<PvpPlayerData> LoadAsync(CancellationToken token)
        {
            _current = _repo.Load<PvpPlayerData>(PvpEs3Keys.PlayerData);
            return UniTask.FromResult(_current);
        }

        public UniTask SaveAsync(PvpPlayerData data, CancellationToken token)
        {
            _current = data;
            _repo.Save(PvpEs3Keys.PlayerData, data);
            return UniTask.CompletedTask;
        }

        public PvpPlayerData GetCurrent()
        {
            return _current ??= _repo.Load<PvpPlayerData>(PvpEs3Keys.PlayerData);
        }

        public void ConsumeTicket(int count = 1)
        {
            if (count <= 0) return;
            var p = GetCurrent();
            p.ArenaTicket = Mathf.Max(0, p.ArenaTicket - count);
            _repo.Save(PvpEs3Keys.PlayerData, p);
        }

        public void AddTickets(int count)
        {
            if (count <= 0) return;
            var p = GetCurrent();
            p.ArenaTicket = Mathf.Min(PvpDefaults.MaxTicketsCap, p.ArenaTicket + count);
            _repo.Save(PvpEs3Keys.PlayerData, p);
        }

        public void AddArenaToken(long amount)
        {
            if (amount <= 0) return;
            var p = GetCurrent();
            p.ArenaToken += amount;
            _repo.Save(PvpEs3Keys.PlayerData, p);
        }
    }
}
