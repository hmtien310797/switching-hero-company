using Battle;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp
{
    /// <summary>
    /// Transition vào PvP: clear PvE chapter stage (creep + boss + live hero) để PvP battle sạch, không
    /// trùng/duplicate hero. Gọi trong <see cref="Battle.PvpRealBattleController.RunAsync"/> TRƯỚC khi
    /// spawn hero PvP (sau khi snapshot đã build từ live hero stats trong matchmaking — nên despawn
    /// live hero lúc này không mất dữ liệu attacker).
    /// <para><see cref="PvEBattleController.CleanupBattle"/>(despawnHeroes:true) despawn creep/boss +
    /// live hero + clear PvE target registry + dispose creep pools.</para>
    /// <para><b>FLAGGED:</b> no-op nếu scene không có PvEBattleController (PvP-dedicated scene). Sau PvP,
    /// PvE stage đã clear — reload scene / restart PvE stage để resume chapter.</para>
    /// </summary>
    public static class PvpStageTransition
    {
        public static void ClearPveStage()
        {
            var pve = PvEBattleController.Instance;
            if (pve == null)
            {
                Debug.Log("[PvP] No PvEBattleController — skipping PvE clear (PvP-dedicated scene?).");
                return;
            }

            pve.CleanupBattle(despawnHeroes: true);
            Debug.Log("[PvP] PvE chapter stage cleared (creep/boss + live hero despawned).");
        }
    }
}
