using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Pvp.Battle;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Repositories;

namespace Immortal_Switch.Scripts.Pvp.Verify
{
    /// <summary>
    /// Local verifier (DOCX §36 — "Local Verifier Simulating the Server"). <b>Không phải security
    /// thật</b> — ép client produce correct request/summaries/validation cho server sau. Kiểm tra
    /// BattleId/version/duration/HP/result/damage-heal-cast tolerances/idempotency.
    /// <b>FLAGGED:</b> MaxHp bound (§36 "0 ≤ FinalHp ≤ MaxHp") cần snapshot MaxHp — M8 chỉ kiểm
    /// FinalHp≥0 + IsDead consistency; full bound khi verify với snapshot.
    /// </summary>
    public static class LocalPvpBattleVerifier
    {
        public static VerificationReport Verify(PvPBattleResultRequest request, IPvPRepository repo, PvPVerificationConfig config)
        {
            var report = new VerificationReport();
            if (request == null) { report.Add("Request", false, "Request is null."); return report; }
            if (config == null) config = new PvPVerificationConfig();

            var pending = repo.Load<PvpPendingBattleData>(PvpEs3Keys.PendingBattle);

            bool battleIdMatches = pending != null && pending.BattleId == request.BattleId;
            report.Add("BattleId", battleIdMatches, battleIdMatches ? "Matches pending battle." : "No matching pending battle.");
            report.Add("Idempotency", pending == null || !pending.IsResolved,
                pending != null && pending.IsResolved ? "Already resolved — re-submit returns prior result." : "Not yet resolved.");

            report.Add("SnapshotVersion", request.SnapshotVersion == 1, $"SnapshotVersion={request.SnapshotVersion}.");
            report.Add("BattleRulesVersion", request.BattleRulesVersion == 1, $"BattleRulesVersion={request.BattleRulesVersion}.");

            float maxDurationMs = PvpDefaults.MaxBattleDurationTicks * PvpDefaults.TickDurationSeconds * 1000f;
            bool durOk = request.DurationMs <= maxDurationMs + config.DurationToleranceSeconds * 1000f;
            report.Add("Duration", durOk, $"Duration {request.DurationMs}ms (max+tol {maxDurationMs + config.DurationToleranceSeconds * 1000f}ms).");

            VerifySummaries(request.AttackerHeroes, config, report, "Attacker");
            VerifySummaries(request.DefenderHeroes, config, report, "Defender");

            return report;
        }

        private static void VerifySummaries(HeroBattleResultSummary[] heroes, PvPVerificationConfig config,
            VerificationReport report, string label)
        {
            if (heroes == null) { report.Add($"{label}.Heroes", false, "No summaries."); return; }

            foreach (var h in heroes)
            {
                if (h == null) continue;

                bool hpOk = h.FinalHp >= 0;
                bool deadConsistent = (h.FinalHp <= 0) == h.IsDead;   // FinalHp<=0 ↔ IsDead (§36 result vs alive/dead).
                report.Add($"{label}.Hero{h.HeroId}.HP", hpOk && deadConsistent,
                    $"FinalHp={h.FinalHp} IsDead={h.IsDead}");

                bool countsOk = h.BasicAttackCount >= 0 && h.ClassSkillCastCount >= 0 && h.UltimateCastCount >= 0;
                report.Add($"{label}.Hero{h.HeroId}.Counts", countsOk,
                    $"basic={h.BasicAttackCount}(tol±{config.BasicAttackCountTolerance}) skill={h.ClassSkillCastCount} ult={h.UltimateCastCount}");
            }
        }
    }

    public sealed class VerificationReport
    {
        public List<VerificationCheck> Checks = new();

        public bool AllPassed
        {
            get
            {
                for (int i = 0; i < Checks.Count; i++)
                    if (!Checks[i].Passed) return false;
                return true;
            }
        }

        public void Add(string name, bool passed, string detail) =>
            Checks.Add(new VerificationCheck { Name = name, Passed = passed, Detail = detail });

        public override string ToString()
        {
            if (AllPassed) return "All verification checks passed.";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < Checks.Count; i++)
            {
                var c = Checks[i];
                sb.Append(c.Passed ? "PASS " : "FAIL ").Append(c.Name).Append(": ").Append(c.Detail).Append('\n');
            }
            return sb.ToString();
        }
    }

    [Serializable]
    public sealed class VerificationCheck
    {
        public string Name;
        public bool Passed;
        public string Detail;
    }
}
