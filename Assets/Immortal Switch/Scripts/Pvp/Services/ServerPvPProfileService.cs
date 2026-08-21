using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Models;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Services
{
    /// <summary>
    /// Server-backed profile service — thay <see cref="LocalPvPProfileService"/>. Rank/tier/vé/
    /// arena_token là nguồn sự thật từ pvp/state (xem handler/pvp.js rpcPvpState), không còn ES3 local.
    /// <see cref="ConsumeTicket"/>/<see cref="AddTickets"/>/<see cref="AddArenaToken"/> (từ
    /// <see cref="IPvPProfileService"/>) chỉ optimistic-update cache cục bộ cho UI phản hồi ngay —
    /// số thật luôn do server trả lại qua pvp/matchmaking và pvp/battle/end, 2 RPC đó gọi
    /// <see cref="ApplyState"/> ngay sau khi có response để cache khớp lại với server thay vì tin
    /// optimistic update (xem ServerPvPMatchmakingService/ServerPvpBattleResultService).
    /// </summary>
    internal sealed class ServerPvPProfileService : IPvPProfileService
    {
        private PvpPlayerData _current;

        public async UniTask<PvpPlayerData> LoadAsync(CancellationToken token)
        {
            var response = await NakamaClient.Instance.PvpStateAsync();
            _current = MapToPlayerData(response, _current);
            return _current;
        }

        public UniTask SaveAsync(PvpPlayerData data, CancellationToken token)
        {
            // Server là nguồn sự thật cho rank/tier/vé/token — không có RPC "save toàn bộ profile".
            // Chỉ cập nhật cache cục bộ (vd DisplayName/milestone list mà server chưa quản lý).
            _current = data;
            return UniTask.CompletedTask;
        }

        public PvpPlayerData GetCurrent()
        {
            // Không lazy-load đồng bộ như bản ES3 cũ (không thể gọi RPC đồng bộ) — PvpManager.
            // InitializeAsync luôn await LoadAsync() trước khi bất kỳ ai gọi GetCurrent(), nên
            // trường hợp null ở đây chỉ xảy ra nếu có lỗi khởi tạo.
            return _current ??= new PvpPlayerData();
        }

        public void ConsumeTicket(int count = 1)
        {
            if (count <= 0) return;
            var p = GetCurrent();
            p.ArenaTicket = Mathf.Max(0, p.ArenaTicket - count);
        }

        public void AddTickets(int count)
        {
            if (count <= 0) return;
            var p = GetCurrent();
            p.ArenaTicket = Mathf.Min(PvpDefaults.MaxTicketsCap, p.ArenaTicket + count);
        }

        public void AddArenaToken(long amount)
        {
            if (amount <= 0) return;
            var p = GetCurrent();
            p.ArenaToken += amount;
        }

        /// <summary>Đồng bộ cache từ 1 response pvp/state sẵn có (tránh round-trip RPC thêm) — gọi
        /// bởi ServerPvPMatchmakingService/ServerPvpBattleResultService ngay sau action đổi rank/vé/
        /// token của họ, để UI đọc <see cref="GetCurrent"/> thấy số mới ngay lập tức.</summary>
        internal void ApplyState(PvpStateResponse response)
        {
            _current = MapToPlayerData(response, _current);
        }

        private static PvpPlayerData MapToPlayerData(PvpStateResponse response, PvpPlayerData previous)
        {
            var data = previous ?? new PvpPlayerData();
            data.RankPoint = response.RankPoints;
            data.RankTier = TierNameToEnum(response.TierName);
            data.ArenaTicket = response.Tickets;
            data.ArenaTicketNextRecoverUnix = response.NextTicketAt ?? 0;
            data.ArenaToken = response.ArenaToken;
            return data;
        }

        private static PvpRankTier TierNameToEnum(string tierName)
        {
            switch (tierName)
            {
                case "silver":   return PvpRankTier.Silver;
                case "gold":     return PvpRankTier.Gold;
                case "platinum": return PvpRankTier.Platinum;
                case "diamond":  return PvpRankTier.Diamond;
                default:         return PvpRankTier.Bronze;
            }
        }
    }
}
