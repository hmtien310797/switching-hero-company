using System;
using System.Collections.Generic;
using System.Threading;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.GrowthSystem;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Mock;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.DevTools;
using Immortal_Switch.Scripts.Pvp.Repositories;
using Immortal_Switch.Scripts.Pvp.Snapshot;
using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.TransmutationSystem;
using Newtonsoft.Json;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Services
{
    /// <summary>
    /// Server-backed matchmaking — thay <see cref="MockPvpMatchmakingService"/>. Ticket consumption
    /// + BattleId + RandomSeed giờ do server cấp (pvp/matchmaking, xem handler/pvp.js
    /// rpcPvpMatchmaking) thay vì local — server không cho tạo trận khi hết vé (NOT_ENOUGH_TICKETS),
    /// khác với bản Local trước đây tự trừ vé không qua kiểm tra network.
    /// <para>
    /// Server ghép người chơi thật gần rank_points khi tìm được (qua leaderboard pvp_rank, xem
    /// findRealOpponent trong handler/pvp.js) — <see cref="PvpOpponentSnapshot.IsBot"/> == false và
    /// <see cref="PvpOpponentSnapshot.Heroes"/> có data hero_id/tier/star/equipment/skill thật của
    /// họ. Snapshot đối thủ dùng trực tiếp <see cref="Immortal_Switch.Scripts.Pvp.DevTools.DefenderTestStatsBuilder"/>
    /// (cùng pipeline dev tool "Defender Hero Test" đã dùng — build base stat từ tier/star + apply
    /// weapon modifier thật) để build snapshot chiến đấu thật, KHÔNG chỉ hiển thị baseline sức mạnh
    /// nữa. Growth/Transmutation của đối thủ ĐÃ được hydrate và áp riêng — xem
    /// <see cref="BuildOpponentGrowthSaveData"/> + PvpRealBattleController.
    /// ApplyOpponentGrowthAndTransmutation (dev-tool pipeline DefenderTestStatsBuilder tự nó không
    /// đọc 2 field này, nên phải apply thêm sau khi build snapshot, không lồng vào
    /// DefenderSlotConfig). Chỉ khi server trả bot (IsBot == true, không tìm được người chơi thật
    /// gần rank) mới rơi về đối thủ giả cục bộ (MockPvpOpponentGenerator, y hệt bản Local) — dev
    /// tool config (nếu Enabled) vẫn ưu tiên cao nhất cho việc test thủ công.
    /// </para>
    /// <para>
    /// BattleId/RandomSeed dùng trong <see cref="HeroVsHeroBattleSnapshot"/> LẤY TỪ SERVER thay vì
    /// tự sinh, để battle_id khớp với pending battle server đang giữ khi submit result.
    /// </para>
    /// </summary>
    internal sealed class ServerPvPMatchmakingService : IPvPMatchmakingService
    {
        private readonly IPvPRepository _repo;
        private readonly IPvPFormationService _formation;
        private readonly IPvPProfileService _profile;
        private readonly IPvPBuffInventoryService _buffInventory;

        public ServerPvPMatchmakingService(IPvPRepository repo, IPvPFormationService formation,
            IPvPProfileService profile, IPvPBuffInventoryService buffInventory)
        {
            _repo = repo;
            _formation = formation;
            _profile = profile;
            _buffInventory = buffInventory;
        }

        public async UniTask<HeroVsHeroBattleSnapshot> FindMatchAsync(CancellationToken token)
        {
            // 1. Validate formation trước khi tốn 1 lượt gọi server (ticket check thật do server làm
            //    ngay bên dưới — không còn tin _profile.GetCurrent().ArenaTicket cục bộ nữa).
            var formation = _formation.LoadFormation();
            var validation = _formation.Validate(formation);
            if (!validation.IsValid)
                throw new InvalidOperationException($"Cannot find match — invalid formation: {validation}");

            // 2. Server trừ vé + cấp BattleId + RandomSeed (server-authoritative, thay bước local
            //    consume ticket + PvpBattleIdFactory/RandomSeedFactory của bản Mock).
            var mm = await NakamaClient.Instance.PvpMatchmakingAsync();
            if (!mm.Success)
                throw new InvalidOperationException($"Cannot find match — server rejected: {mm.Error}");

            if (_profile is ServerPvPProfileService serverProfile)
            {
                var current = serverProfile.GetCurrent();
                current.ArenaTicket = mm.Tickets;
            }

            // 3. Build player team snapshot từ in-battle HeroActors + formation buff loadout.
            var playerTeam = BuildPlayerTeamSnapshot(formation);

            // 4. Build opponent team snapshot — ưu tiên: dev tool config (test thủ công) > người
            //    chơi thật server ghép được (mm.Opponent.Heroes) > mock giả (server trả bot).
            var defenderConfig = DefenderHeroTestConfigSO.LoadOrCreate();
            TeamBattleSnapshot opponentTeam;
            if (defenderConfig != null && defenderConfig.Enabled)
            {
                opponentTeam = BuildOpponentFromConfig(defenderConfig);
            }
            else if (!mm.Opponent.IsBot && HasRealHeroData(mm.Opponent))
            {
                opponentTeam = BuildRealOpponentTeamSnapshot(mm.Opponent);
            }
            else
            {
                var opponentSave = SelectOrGenerateOpponent(_profile.GetCurrent()?.RankPoint ?? 0);
                opponentTeam = BuildOpponentTeamSnapshot(opponentSave);
            }

            // 5. BattleId + RandomSeed từ SERVER (không tự sinh — phải khớp pending battle server giữ).
            var snapshot = new HeroVsHeroBattleSnapshot
            {
                BattleId = mm.BattleId,
                SnapshotVersion = 1,
                BattleRulesVersion = 1,
                RandomSeed = mm.RandomSeed,
                Attacker = playerTeam,
                Defender = opponentTeam
            };

            // 6. Save PendingBattle cục bộ (giữ cho History/QA export như bản Local — idempotency
            //    thật giờ nằm ở server qua battle_id, cái này chỉ để UI/debug).
            var pending = new PvpPendingBattleData
            {
                BattleId = mm.BattleId,
                RandomSeed = mm.RandomSeed,
                BattleRulesVersion = 1,
                SnapshotVersion = 1,
                CreatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IsResolved = false,
                IsSurrendered = false,
                SnapshotJson = JsonConvert.SerializeObject(snapshot)
            };
            _repo.Save(PvpEs3Keys.PendingBattle, pending);

            GameEventManager.Trigger(GameEvents.ON_PVP_MATCH_FOUND);
            return snapshot;
        }

        private PvpMockOpponentSave SelectOrGenerateOpponent(int playerRankPoint)
        {
            var data = _repo.Load<PvpMockOpponentData>(PvpEs3Keys.MockOpponents);
            if (data.Opponents == null || data.Opponents.Count == 0)
            {
                data.Opponents = MockPvpOpponentGenerator.Generate(MockPvpOpponentGenerator.DefaultCount, playerRankPoint);
                _repo.Save(PvpEs3Keys.MockOpponents, data);
            }

            var rng = new System.Random();
            return data.Opponents[rng.Next(data.Opponents.Count)];
        }

        private TeamBattleSnapshot BuildPlayerTeamSnapshot(PvpFormationSaveData formation)
        {
            var cache = UserDataCache.Instance;
            var frontActor = cache?.GetInBattleHeroActorAt(0);
            var backActor = cache?.GetInBattleHeroActorAt(1);

            HeroBattleSnapshot front = frontActor != null
                ? HeroBattleSnapshotBuilder.BuildForPlayer(frontActor, FormationSlot.Front,
                    formation.FrontLoadout, _buffInventory)
                : null;
            HeroBattleSnapshot back = backActor != null
                ? HeroBattleSnapshotBuilder.BuildForPlayer(backActor, FormationSlot.Back,
                    formation.BackLoadout, _buffInventory)
                : null;

            long power = PvpTeamPowerEstimator.EstimateTeam(front?.FinalStats, back?.FinalStats);
            return new TeamBattleSnapshot { FrontHero = front, BackHero = back, TeamPower = power };
        }

        private TeamBattleSnapshot BuildOpponentTeamSnapshot(PvpMockOpponentSave save)
        {
            MockPvPOpponentProfile profile = null;
            try
            {
                if (!string.IsNullOrEmpty(save?.ProgressionJson))
                    profile = JsonConvert.DeserializeObject<MockPvPOpponentProfile>(save.ProgressionJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PvP] Failed to deserialize mock opponent profile {save?.OpponentId}: {e.Message}");
            }

            if (profile == null)
                return new TeamBattleSnapshot { TeamPower = save?.TeamPower ?? 0 };

            var front = MockToSnapshot(profile.FrontHero, FormationSlot.Front);
            var back = MockToSnapshot(profile.BackHero, FormationSlot.Back);
            return new TeamBattleSnapshot { FrontHero = front, BackHero = back, TeamPower = profile.TeamPower };
        }

        private static HeroBattleSnapshot MockToSnapshot(MockHeroProgressionData mock, FormationSlot slot)
        {
            if (mock == null) return null;

            var ctx = new HeroSnapshotBuildContext
            {
                HeroId = mock.HeroId,
                AssignedSlot = slot,
                Level = mock.Level,
                Star = mock.Star,
                Tier = (HeroProgressTier)mock.Tier,
                Stats = null,
                PrebuiltFinalStats = mock.FinalStats,
                Equipment = mock.Equipment,
                Skills = mock.Skills,
                Growth = mock.Growth,
                Transmutation = mock.Transmutation,
                FormationBuffs = mock.FormationBuffs ?? new List<FormationBuffSnapshot>()
            };
            return HeroBattleSnapshotBuilder.Build(ctx);
        }

        private static TeamBattleSnapshot BuildOpponentFromConfig(DefenderHeroTestConfigSO config)
        {
            var front = DefenderTestStatsBuilder.BuildHeroSnapshot(config.Front, FormationSlot.Front);
            var back = DefenderTestStatsBuilder.BuildHeroSnapshot(config.Back, FormationSlot.Back);

            long power = PvpTeamPowerEstimator.EstimateTeam(front?.FinalStats, back?.FinalStats);
            return new TeamBattleSnapshot { FrontHero = front, BackHero = back, TeamPower = power };
        }

        private static bool HasRealHeroData(PvpOpponentSnapshot opponent)
        {
            if (opponent?.Heroes == null) return false;
            foreach (var h in opponent.Heroes)
            {
                if (h != null && h.HeroId > 0) return true;
            }
            return false;
        }

        // Đối thủ thật server ghép được (findRealOpponent, xem handler/pvp.js) — dùng chung pipeline
        // với dev tool "Defender Hero Test" (DefenderTestStatsBuilder: base stat từ tier/star +
        // weapon modifier thật), chỉ khác nguồn config đến từ server thay vì asset chỉnh tay.
        private static TeamBattleSnapshot BuildRealOpponentTeamSnapshot(PvpOpponentSnapshot opponent)
        {
            var heroes = opponent.Heroes;
            var frontLoadout = heroes != null && heroes.Count > 0 ? heroes[0] : null;
            var backLoadout = heroes != null && heroes.Count > 1 ? heroes[1] : null;

            var front = BuildDefenderSnapshotFromServerLoadout(frontLoadout, FormationSlot.Front);
            var back = BuildDefenderSnapshotFromServerLoadout(backLoadout, FormationSlot.Back);

            long power = PvpTeamPowerEstimator.EstimateTeam(front?.FinalStats, back?.FinalStats);
            return new TeamBattleSnapshot
            {
                FrontHero = front,
                BackHero = back,
                TeamPower = power,
                OpponentGrowth = BuildOpponentGrowthSaveData(opponent.Growth),
                OpponentTransmutationModifiers = TransmutationSystemHelper.ToModifiers(opponent.TransmutationModifiers)
            };
        }

        // GrowthSaveData shape (CurrentUnlockedTier + Stats:[{Stat,CurrentStack}]) khớp 1-1 với
        // PvpOpponentGrowth server trả — chỉ cần parse Stat string -> enum, không cần tính toán gì.
        // GrowthSystemService (đăng ký làm IPowerUpSource) tự resolve percent-per-stack thật khi
        // apply lên StatModule (xem ApplyOpponentGrowthAndTransmutation, PvpRealBattleController.cs).
        private static GrowthSaveData BuildOpponentGrowthSaveData(PvpOpponentGrowth growth)
        {
            if (growth == null) return null;

            var data = new GrowthSaveData { CurrentUnlockedTier = Mathf.Max(1, growth.CurrentUnlockedTier) };
            if (growth.Stats != null)
            {
                foreach (var s in growth.Stats)
                {
                    if (s == null) continue;
                    if (Enum.TryParse<StatType>(s.Stat, true, out var stat))
                        data.SetStack(stat, Mathf.Max(0, s.CurrentStack));
                    else
                        Debug.LogWarning($"[PvP] Unknown growth stat '{s.Stat}' from server for real opponent.");
                }
            }
            return data;
        }

        private static HeroBattleSnapshot BuildDefenderSnapshotFromServerLoadout(PvpOpponentHeroLoadout loadout, FormationSlot slot)
        {
            if (loadout == null || loadout.HeroId <= 0) return null;

            var config = new DefenderSlotConfig
            {
                HeroId = loadout.HeroId,
                Tier = (HeroProgressTier)loadout.Tier,
                Star = loadout.Star,
                Level = Mathf.Max(1, loadout.Level),
                StandardWeaponId = loadout.Equipment?.StandardWeaponId ?? 0,
                StandardWeaponLevel = Mathf.Max(1, loadout.Equipment?.StandardWeaponLevel ?? 1),
                ExclusiveWeaponId = loadout.Equipment?.ExclusiveWeaponId ?? 0,
                ExclusiveWeaponLevel = Mathf.Max(1, loadout.Equipment?.ExclusiveWeaponLevel ?? 1),
                ExclusiveWeaponStar = Mathf.Max(1, loadout.Equipment?.ExclusiveWeaponStar ?? 1),
                UseExclusive = loadout.Equipment?.UseExclusive ?? false,
                Skills = new List<SkillEntry>()
            };

            if (loadout.Skills?.Equipped != null)
            {
                foreach (var s in loadout.Skills.Equipped)
                {
                    if (s == null || s.SkillId <= 0) continue;
                    config.Skills.Add(new SkillEntry { SkillId = s.SkillId, SkillLevel = Mathf.Max(1, s.Level) });
                }
            }

            return DefenderTestStatsBuilder.BuildHeroSnapshot(config, slot);
        }
    }
}
