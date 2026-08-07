using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Services
{
    /// <summary>
    /// Phase-1 mock leaderboard (bảng xếp hạng PvP Main). Server chưa làm → sinh data giả 50 record
    /// + top1/2/3 + myRank. <b>TODO server:</b> thay impl này bằng service gọi RPC (giữ interface
    /// <see cref="IPvPLeaderboardService"/>), server sẽ trả các field thật.
    /// <para>Dùng RNG seeded cố định để data ổn định trong session (không đổi mỗi lần mở).</para>
    /// </summary>
    internal sealed class LocalPvPLeaderboardService : IPvPLeaderboardService
    {
        private const int RankingsCount = 50;
        private const int Seed = 20260805;

        private readonly IPvPProfileService _profile;
        private PvpLeaderboardResponseModel _cached;

        public LocalPvPLeaderboardService(IPvPProfileService profile)
        {
            _profile = profile;
        }

        public UniTask<PvpLeaderboardResponseModel> GetLeaderboardAsync(CancellationToken token)
        {
            if (_cached != null)
                return UniTask.FromResult(_cached);

            _cached = BuildMock();
            return UniTask.FromResult(_cached);
        }

        private PvpLeaderboardResponseModel BuildMock()
        {
            var rng = new System.Random(Seed);
            var rankings = new List<PvpLeaderboardEntryModel>(RankingsCount);

            for (int i = 0; i < RankingsCount; i++)
            {
                int rank = i + 1;
                rankings.Add(new PvpLeaderboardEntryModel
                {
                    PlayerId = $"player_{rank:D3}",
                    Nickname = RandomNickname(rng, rank),
                    Rank = rank,
                    RankPoints = Math.Max(50, 2500 - rank * 37 + rng.Next(-15, 15)),
                    FormationPower = RandomPower(rng),
                    RankTierId = TierIdByRank(rank),
                    RankTierLevel = rng.Next(1, 4),
                    FormationHeroes = BuildHeroes(rng)
                });
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // TODO server: weeklyResetAtUtc thật (server tính theo season/weekly). Mock: +7 ngày.
            var resp = new PvpLeaderboardResponseModel
            {
                ServerTimeUtc = now,
                WeeklyResetAtUtc = now + 7 * 24 * 3600,
                Top1 = rankings[0],
                Top2 = rankings[1],
                Top3 = rankings[2],
                Rankings = rankings,
                MyRank = BuildMyRank()
            };
            return resp;
        }

        /// <summary>myRank: lấy nickname/points/tier từ profile local thật; rank (vị trí) mock.</summary>
        private PvpLeaderboardEntryModel BuildMyRank()
        {
            var profile = _profile?.GetCurrent();
            return new PvpLeaderboardEntryModel
            {
                PlayerId = "current_player",
                Nickname = string.IsNullOrEmpty(profile?.DisplayName) ? "PlayerName" : profile.DisplayName,
                // TODO server: rank thật do server tính.
                Rank = 22,
                RankPoints = profile?.RankPoint ?? 0,
                FormationPower = "108200000000000",
                RankTierId = TierIdToString(profile?.RankTier ?? PvpRankTier.Bronze),
                RankTierLevel = 1,
                IsRanked = true
            };
        }

        private static string RandomNickname(System.Random rng, int rank)
        {
            string[] pool =
            {
                "XKhuTook", "DanP", "KKkaoozzzUU", "NightFury", "ShadowBlade", "IceQueen",
                "DragonSlayer", "PixelWarrior", "MysticFox", "IronFist", "StarSeeker", "ThunderBoltz",
                "DarkOracle", "GoldenHammer", "SilentStorm", "FrostNova", "BlazeRider", "GhostWraith",
                "LunarTiger", "CrimsonDawn", "AeroViper", "VenomStrike", "SolarFlare", "NebulaKnight",
                "ToxicReaper", "WildHawk", "Stormbringer", "Emberlynx", "TurboCharge", "FeralWolfe"
            };
            string baseName = pool[(rank + rng.Next(0, 3)) % pool.Length];
            return rng.Next(0, 100) < 70 ? baseName : $"{baseName}{rng.Next(1, 100)}";
        }

        private static List<PvpLeaderboardHeroModel> BuildHeroes(System.Random rng)
        {
            int[] heroIds = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10,11,12,13 };
            string[] heroTier = { "Common", "UnCommon", "Rare", "Epic", "Legendary", "Mythic" };
            return new List<PvpLeaderboardHeroModel>(2)
            {
                new PvpLeaderboardHeroModel
                {
                    HeroId = heroIds[rng.Next(heroIds.Length)],
                    FormationSlot = 0,
                    Star = rng.Next(1, 6),
                    Tier = heroTier[rng.Next(heroTier.Length)]
                },
                new PvpLeaderboardHeroModel
                {
                    HeroId = heroIds[rng.Next(heroIds.Length)],
                    FormationSlot = 1,
                    Star = rng.Next(1, 6),
                    Tier = heroTier[rng.Next(heroTier.Length)]
                }
            };
        }

        private static string RandomPower(System.Random rng)
        {
            long mantissa = rng.Next(100, 999) * 1000L;
            long exponent = rng.Next(0, 5);
            return mantissa.ToString() + new string('0', (int)exponent * 3);
        }

        private static string TierIdByRank(int rank)
        {
            if (rank <= 3) return "diamond";
            if (rank <= 10) return "diamond";
            if (rank <= 20) return "platinum";
            if (rank <= 30) return "gold";
            if (rank <= 40) return "silver";
            return "bronze";
        }

        private static string TierIdToString(PvpRankTier tier)
        {
            switch (tier)
            {
                case PvpRankTier.Bronze: return "bronze";
                case PvpRankTier.Silver: return "silver";
                case PvpRankTier.Gold: return "gold";
                case PvpRankTier.Platinum: return "platinum";
                case PvpRankTier.Diamond: return "diamond";
                default: return "bronze";
            }
        }
    }
}