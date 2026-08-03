using System;
using System.Collections.Generic;
using Common;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Snapshot;
using Immortal_Switch.Scripts.StatSystem;
using Newtonsoft.Json;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Mock
{
    /// <summary>
    /// Sinh mock opponent local (DOCX §30). 10–20 opponents, lower/equal/higher power band quanh
    /// player, stable seed từ OpponentId. Hero pool = player owned heroes (valid HeroInstance stats).
    /// <b>FLAGGED:</b> Phase-1 mock — FinalStats synthesized từ HeroInstance base stats × band mult,
    /// không phải full progression rebuild. Production server cung cấp opponent thật.
    /// </summary>
    internal static class MockPvpOpponentGenerator
    {
        public const int DefaultCount = 12;

        public static List<PvpMockOpponentSave> Generate(int count, int playerRankPoint)
        {
            int n = Math.Max(1, count);
            var result = new List<PvpMockOpponentSave>(n);
            var pool = GetHeroPool();

            for (int i = 0; i < n; i++)
            {
                var band = (MockOpponentPowerBand)(i % 3);
                string opponentId = $"mock-{i:00}";
                var profile = GenerateProfile(opponentId, $"MockPlayer{i:00}", band, playerRankPoint, pool);
                result.Add(ToSave(profile, band));
            }
            return result;
        }

        private static HeroInstance[] GetHeroPool()
        {
            var owned = UserDataCache.Instance?.HeroList?.Owned;
            if (owned != null && owned.Length > 0) return owned;
            Debug.LogWarning("[PvP] MockPvpOpponentGenerator: player owns no heroes — mock opponents use fallback heroId -1.");
            return Array.Empty<HeroInstance>();
        }

        private static MockPvPOpponentProfile GenerateProfile(string opponentId, string displayName,
            MockOpponentPowerBand band, int playerRankPoint, HeroInstance[] pool)
        {
            ulong seed = RandomSeedFactory.StableSeed(opponentId);
            var rng = new System.Random((int)(seed % int.MaxValue));

            float mult = band switch
            {
                MockOpponentPowerBand.Lower => 0.75f,
                MockOpponentPowerBand.Higher => 1.3f,
                _ => 1.0f
            };

            int rankOffset = band switch
            {
                MockOpponentPowerBand.Lower => -150,
                MockOpponentPowerBand.Higher => 150,
                _ => 0
            };

            var front = PickMockHero(pool, rng, mult);
            var back = PickMockHero(pool, rng, mult, front?.HeroId ?? -1);

            return new MockPvPOpponentProfile
            {
                OpponentId = opponentId,
                DisplayName = displayName,
                RankPoint = Math.Max(0, playerRankPoint + rankOffset),
                TeamPower = PvpTeamPowerEstimator.EstimateTeam(front?.FinalStats, back?.FinalStats),
                FrontHero = front,
                BackHero = back,
                Formation = BuildMockFormation(front, back),
                DataVersion = 1
            };
        }

        private static MockHeroProgressionData PickMockHero(HeroInstance[] pool, System.Random rng, float mult, int excludeHeroId = -1)
        {
            if (pool == null || pool.Length == 0)
                return new MockHeroProgressionData
                {
                    HeroId = -1, Star = 1, Tier = 0, Level = 1, FinalStats = new RuntimeStatSnapshot()
                };

            HeroInstance h = null;
            for (int tries = 0; tries < pool.Length; tries++)
            {
                var cand = pool[rng.Next(pool.Length)];
                if (cand != null && cand.HeroId != excludeHeroId) { h = cand; break; }
                if (cand != null && h == null) h = cand;
            }

            if (h == null)
                return new MockHeroProgressionData { HeroId = -1, Star = 1, Tier = 0, Level = 1 };

            return new MockHeroProgressionData
            {
                HeroId = h.HeroId,
                Star = Math.Max(1, h.Star),
                Tier = MapRarityToTier(h.Rarity),
                Level = h.Level,
                FinalStats = SynthesizeFinalStats(h, mult)
            };
        }

        private static RuntimeStatSnapshot SynthesizeFinalStats(HeroInstance h, float mult)
        {
            var s = new RuntimeStatSnapshot();
            s.Set(StatType.MaxHp, h.Hp * mult);
            s.Set(StatType.Atk, h.Atk * mult);
            s.Set(StatType.Def, h.Def * mult);
            s.Set(StatType.CritChance, h.CritChance);
            s.Set(StatType.CritDamage, h.CritDamage);
            s.Set(StatType.AttackSpeed, h.AtkSpd);
            s.Set(StatType.AttackRange, h.AttackRange);
            s.Set(StatType.Accuracy, 1f);
            s.Set(StatType.MoveSpeed, 1f);
            return s;
        }

        private static int MapRarityToTier(string rarity)
        {
            return rarity switch
            {
                "Common" => 0,
                "Rare" or "UnCommon" or "Uncommon" => 1,
                "Epic" => 2,
                "Legendary" => 3,
                "Mythic" => 4,
                _ => 0
            };
        }

        private static PvpFormationSaveData BuildMockFormation(MockHeroProgressionData front, MockHeroProgressionData back)
        {
            var f = new PvpFormationSaveData
            {
                FrontHeroId = front?.HeroId ?? -1,
                BackHeroId = back?.HeroId ?? -1
            };
            f.FrontLoadout.Core = PvpTestBuffIds.IronCore;
            f.BackLoadout.Core = PvpTestBuffIds.IronCore;
            return f;
        }

        private static PvpMockOpponentSave ToSave(MockPvPOpponentProfile profile, MockOpponentPowerBand band)
        {
            return new PvpMockOpponentSave
            {
                OpponentId = profile.OpponentId,
                DisplayName = profile.DisplayName,
                RankPoint = profile.RankPoint,
                TeamPower = profile.TeamPower,
                PowerBand = band,
                FrontHeroId = profile.FrontHero?.HeroId ?? -1,
                BackHeroId = profile.BackHero?.HeroId ?? -1,
                FrontLoadout = profile.Formation?.FrontLoadout,
                BackLoadout = profile.Formation?.BackLoadout,
                ProgressionJson = JsonConvert.SerializeObject(profile),
                DataVersion = profile.DataVersion
            };
        }
    }
}
