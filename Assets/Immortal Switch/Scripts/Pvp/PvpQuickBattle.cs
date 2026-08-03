using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Battle;
using Immortal_Switch.Scripts.Pvp.Snapshot;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp
{
    /// <summary>
    /// Code-only entry vào PvP battle (KHÔNG UI). Flow:
    /// <para>1. Ensure <see cref="PvpManager"/> init (nếu chưa qua GameBootstrap).</para>
    /// <para>2. Validate formation + ticket (matchmaking sẽ throw nếu thiếu).</para>
    /// <para>3. <c>Matchmaking.FindMatchAsync</c> → immutable <see cref="HeroVsHeroBattleSnapshot"/> (consume 1 ticket, save PendingBattle).</para>
    /// <para>4. <see cref="PvpRealBattleController.RunAsync"/>(snapshot, openUi=false) → spawn 4 hero, auto-end trong Update; kết quả qua <see cref="PvpRealBattleController.OnBattleEnded"/> + ES3 rank/reward/history.</para>
    /// <para><b>Yêu cầu:</b> scene có player hero đã load (MainBattleScene post-bootstrap) + matrix Bullet×Player ON + hero collider Player + bullet trigger+Rigidbody.</para>
    /// </summary>
    public static class PvpQuickBattle
    {
        public static async UniTask StartAsync(bool openHud = false, bool cheatTickets = true, CancellationToken token = default)
        {
            var mgr = PvpManager.Instance;
            if (mgr == null)
            {
                Debug.LogError("[PvP] PvpManager not available.");
                return;
            }

            // PvP services init nếu Facade chưa có (MainBattleScene post-bootstrap thì đã có).
            if (mgr.Facade == null)
                await mgr.InitializeAsync();

            var facade = mgr.Facade;
            if (facade?.Matchmaking == null)
            {
                Debug.LogError("[PvP] Matchmaking not ready.");
                return;
            }

            // Validate formation.
            var formation = facade.Formation.LoadFormation();
            var v = facade.Formation.Validate(formation);
            if (!v.IsValid)
            {
                Debug.LogError($"[PvP] Formation invalid: {v}. Set formation first (UI or bootstrap).");
                return;
            }

            // Ticket. cheatTickets (default true): top-up vé để debug không bị block. Matchmaking vẫn
            // consume 1 vé (DOCX), nhưng top-up đảm bảo luôn có vé.
            var profile = facade.Profile.GetCurrent();
            if (cheatTickets)
            {
                if (profile != null && profile.ArenaTicket < 5)
                    facade.Profile.AddTickets(5);
            }
            else if (profile == null || profile.ArenaTicket <= 0)
            {
                Debug.LogError("[PvP] No Arena ticket. Add via PvpDebugService.AddTickets(5).");
                return;
            }

            // 1) Matchmaking → immutable snapshot (consume ticket, save PendingBattle).
            HeroVsHeroBattleSnapshot snapshot;
            try
            {
                snapshot = await facade.Matchmaking.FindMatchAsync(token);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PvP] Matchmaking failed: {e.Message}");
                return;
            }

            // 2) Real battle (no UI). Auto-ends in Update → OnBattleEnded + ES3 rank/reward.
            var real = PvpRealBattleController.Instance;
            if (real == null)
            {
                Debug.LogError("[PvP] PvpRealBattleController not ready.");
                return;
            }

            await real.RunAsync(snapshot, token, openHud);
        }
    }
}
