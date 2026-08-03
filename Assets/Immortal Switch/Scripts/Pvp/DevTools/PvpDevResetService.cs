using Immortal_Switch.Scripts.Core;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.DevTools
{
    /// <summary>
    /// Dev tool: reset toàn bộ local PvP data (DOCX §6 — "Reset PvP Local Data",
    /// §14 Markdown screen 14). Phase-1 wrapper quanh <see cref="PvpManager.ResetLocalData"/> —
    /// tách class riêng để debug menu (M8) gọi mà không phụ thuộc facade internals.
    /// Namespace dùng <c>DevTools</c> (không dùng <c>Debug</c>) để không shadow <c>UnityEngine.Debug</c>.
    /// </summary>
    public static class PvpDevResetService
    {
        public static void ResetAll()
        {
            if (PvpManager.Instance == null)
            {
                Debug.LogWarning("[PvP] PvpManager not initialized — cannot reset local data.");
                return;
            }

            PvpManager.Instance.ResetLocalData();
        }
    }
}
