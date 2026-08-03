using Immortal_Switch.Scripts.Pvp.DevTools;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Screen 14 — PvP Debug / Mock Tools (Markdown §14). Dev-only.
    ///
    /// PREFAB (Addressable = "PvpDebugView", UILayer.Main, PageExclusive):
    ///   TMP_Text: txtDevBadge, txtOutput
    ///   Button: btnAddToken, btnAddTickets, btnUnlockAll, btnRegenOpponents,
    ///           btnSim1000, btnVerify, btnExport, btnClear, btnClose
    /// </summary>
    public class PvpDebugView : UIView
    {
        [SerializeField] private TMP_Text txtDevBadge;
        [SerializeField] private TMP_Text txtOutput;
        [SerializeField] private Button btnAddToken;
        [SerializeField] private Button btnAddTickets;
        [SerializeField] private Button btnUnlockAll;
        [SerializeField] private Button btnRegenOpponents;
        [SerializeField] private Button btnSim1000;
        [SerializeField] private Button btnVerify;
        [SerializeField] private Button btnExport;
        [SerializeField] private Button btnClear;
        [SerializeField] private Button btnClose;

        private void Awake()
        {
            if (btnAddToken != null) btnAddToken.onClick.AddListener(() => { PvpDebugService.AddArenaToken(5000); Log("+5000 Arena Token"); });
            if (btnAddTickets != null) btnAddTickets.onClick.AddListener(() => { PvpDebugService.AddTickets(5); Log("+5 Tickets"); });
            if (btnUnlockAll != null) btnUnlockAll.onClick.AddListener(() => { PvpDebugService.UnlockAllBuffs(); Log("All buffs unlocked"); });
            if (btnRegenOpponents != null) btnRegenOpponents.onClick.AddListener(() => { PvpDebugService.RegenerateMockOpponents(); Log("Mock opponents regenerated"); });
            if (btnSim1000 != null) btnSim1000.onClick.AddListener(() => Log(PvpDebugService.Simulate1000Rolls()));
            if (btnVerify != null) btnVerify.onClick.AddListener(() => Log(PvpDebugService.VerifyLastResult()));
            if (btnExport != null) btnExport.onClick.AddListener(() =>
            {
                var j = PvpDebugService.ExportSnapshotJson();
                Log(string.IsNullOrEmpty(j) ? "No pending snapshot" : "Snapshot exported (see Console log)");
                if (!string.IsNullOrEmpty(j)) Debug.Log($"[PvP] Snapshot JSON:\n{j}");
            });
            if (btnClear != null) btnClear.onClick.AddListener(() => { PvpDebugService.ClearAllPvpData(); Log("PvP data cleared & re-bootstrap"); });
            if (btnClose != null) btnClose.onClick.AddListener(() => UIManager.Instance.Close<PvpDebugView>());
        }

        public override void OnShow(object args)
        {
            if (txtDevBadge != null) txtDevBadge.text = "DEV ONLY";
            if (txtOutput != null) txtOutput.text = "Select a debug action.";
        }

        private void Log(string msg)
        {
            if (txtOutput != null) txtOutput.text = msg;
            UIManager.Instance.ShowToast(msg);
        }
    }
}
