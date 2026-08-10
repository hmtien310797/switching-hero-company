using System;
using System.Collections.Generic;
using System.Threading;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Mock;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.DevTools;
using Immortal_Switch.Scripts.Pvp.Repositories;
using Immortal_Switch.Scripts.Pvp.Snapshot;
using Newtonsoft.Json;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Services
{
    /// <summary>
    /// Phase-1 local mock matchmaking (DOCX §31). Validate formation+ticket → select mock opponent
    /// (generate nếu rỗng) → consume ticket → tạo BattleId+RandomSeed+immutable snapshot → save
    /// PendingBattle. Player team snapshot từ in-battle HeroActors; opponent team từ mock profile
    /// (deserialize ProgressionJson → same snapshot pipeline).
    /// </summary>
    internal sealed class MockPvpMatchmakingService : IPvPMatchmakingService
    {
        private readonly IPvPRepository _repo;
        private readonly IPvPFormationService _formation;
        private readonly IPvPProfileService _profile;
        private readonly IPvPBuffInventoryService _buffInventory;

        public MockPvpMatchmakingService(IPvPRepository repo, IPvPFormationService formation,
            IPvPProfileService profile, IPvPBuffInventoryService buffInventory)
        {
            _repo = repo;
            _formation = formation;
            _profile = profile;
            _buffInventory = buffInventory;
        }

        public UniTask<HeroVsHeroBattleSnapshot> FindMatchAsync(CancellationToken token)
        {
            // 1. Validate formation + ticket (DOCX §31).
            var formation = _formation.LoadFormation();
            var validation = _formation.Validate(formation);
            if (!validation.IsValid)
                throw new InvalidOperationException($"Cannot find match — invalid formation: {validation}");

            var profile = _profile.GetCurrent();
            if (profile == null || profile.ArenaTicket <= 0)
                throw new InvalidOperationException("No Arena tickets.");

            // 2. Select mock opponent (generate if list empty — DOCX §30/§7 reconciliation).
            var opponentSave = SelectOrGenerateOpponent(profile.RankPoint);

            // 3. Consume ticket (DOCX §31 — "Consume one local ticket"). Sau BattleId tạo, rời = defeat.
            _profile.ConsumeTicket(1);

            // 4. Build player team snapshot từ in-battle HeroActors + formation buff loadout.
            var playerTeam = BuildPlayerTeamSnapshot(formation);

            // 5. Build opponent team snapshot từ mock profile (deserialize ProgressionJson).
            //    Nếu Defender Hero Test config đang enabled thì dùng config để build defender (giả lập
            //    một user khác có tier/star/skill/equipment cụ thể) thay vì mock opponent random.
            var defenderConfig = DefenderHeroTestConfigSO.LoadOrCreate();
            TeamBattleSnapshot opponentTeam = defenderConfig != null && defenderConfig.Enabled
                ? BuildOpponentFromConfig(defenderConfig)
                : BuildOpponentTeamSnapshot(opponentSave);

            // 6. Create BattleId + RandomSeed + immutable snapshot (DOCX §10).
            var battleId = PvpBattleIdFactory.Create();
            var seed = RandomSeedFactory.Create();
            var snapshot = new HeroVsHeroBattleSnapshot
            {
                BattleId = battleId,
                SnapshotVersion = 1,
                BattleRulesVersion = 1,
                RandomSeed = seed,
                Attacker = playerTeam,
                Defender = opponentTeam
            };

            // 7. Save PendingBattle (immutable snapshot as JSON — DOCX §6, §10).
            var pending = new PvpPendingBattleData
            {
                BattleId = battleId,
                RandomSeed = seed,
                BattleRulesVersion = 1,
                SnapshotVersion = 1,
                CreatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IsResolved = false,
                IsSurrendered = false,
                SnapshotJson = JsonConvert.SerializeObject(snapshot)
            };
            _repo.Save(PvpEs3Keys.PendingBattle, pending);

            GameEventManager.Trigger(GameEvents.ON_PVP_MATCH_FOUND);
            return UniTask.FromResult(snapshot);
        }

        private PvpMockOpponentSave SelectOrGenerateOpponent(int playerRankPoint)
        {
            var data = _repo.Load<PvpMockOpponentData>(PvpEs3Keys.MockOpponents);
            if (data.Opponents == null || data.Opponents.Count == 0)
            {
                data.Opponents = MockPvpOpponentGenerator.Generate(MockPvpOpponentGenerator.DefaultCount, playerRankPoint);
                _repo.Save(PvpEs3Keys.MockOpponents, data);
            }

            // No opponent selection screen (DOCX §31). Pick random for Phase-1.
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

        /// <summary>Build defender team từ Defender Hero Test config (giả lập tier/star/skill/equipment).</summary>
        private static TeamBattleSnapshot BuildOpponentFromConfig(DefenderHeroTestConfigSO config)
        {
            var front = DefenderTestStatsBuilder.BuildHeroSnapshot(config.Front, FormationSlot.Front);
            var back = DefenderTestStatsBuilder.BuildHeroSnapshot(config.Back, FormationSlot.Back);

            long power = PvpTeamPowerEstimator.EstimateTeam(front?.FinalStats, back?.FinalStats);
            return new TeamBattleSnapshot { FrontHero = front, BackHero = back, TeamPower = power };
        }
    }
}
