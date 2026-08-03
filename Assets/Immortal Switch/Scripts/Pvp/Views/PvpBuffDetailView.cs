using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Screen 11 — Buff Detail &amp; Upgrade (Markdown §11, wireframe 11). Level, current/next effects,
    /// shards, upgrade req, upgrade button. Upgrade = atomic (DOCX §28).
    ///
    /// PREFAB (Addressable = "PvpBuffDetailView", UILayer.Popup):
    ///   TMP_Text: txtDevBadge, txtName, txtLevel, txtCurrent, txtNext, txtShards, txtToken, txtReq, txtEquipState
    ///   Button: btnUpgrade, btnClose
    /// </summary>
    public class PvpBuffDetailView : UIView
    {
        [SerializeField] private TMP_Text txtDevBadge;
        [SerializeField] private TMP_Text txtName;
        [SerializeField] private TMP_Text txtLevel;
        [SerializeField] private TMP_Text txtCurrent;
        [SerializeField] private TMP_Text txtNext;
        [SerializeField] private TMP_Text txtShards;
        [SerializeField] private TMP_Text txtToken;
        [SerializeField] private TMP_Text txtReq;
        [SerializeField] private TMP_Text txtEquipState;
        [SerializeField] private Button btnUpgrade;
        [SerializeField] private Button btnClose;

        private string _buffId;

        private void Awake()
        {
            if (btnUpgrade != null) btnUpgrade.onClick.AddListener(() => UpgradeAsync().Forget());
            if (btnClose != null) btnClose.onClick.AddListener(() => UIManager.Instance.Close<PvpBuffDetailView>());
        }

        public override void OnShow(object args)
        {
            if (txtDevBadge != null) txtDevBadge.text = "LOCAL MOCK";
            _buffId = args as string;
            RefreshAsync().Forget();
        }

        private async UniTask RefreshAsync()
        {
            var facade = PvpManager.Instance?.Facade;
            if (facade == null || string.IsNullOrEmpty(_buffId)) return;

            if (facade.BuffCatalog.TryGet(_buffId, out var def))
            {
                if (txtName != null) txtName.text = def.BuffNameKey;
            }

            var ob = facade.BuffInventory.GetOwnedBuff(_buffId);
            if (txtLevel != null) txtLevel.text = $"Lv.{ob?.Level ?? 0}";
            if (txtShards != null) txtShards.text = $"Shards: {ob?.Shards ?? 0}";
            if (txtEquipState != null) txtEquipState.text = ob?.IsUnlocked == true ? "Owned" : "Not owned";

            var profile = facade.Profile?.GetCurrent();
            if (txtToken != null && profile != null) txtToken.text = $"Arena Token: {profile.ArenaToken}";

            var preview = await facade.Progression.PreviewUpgradeAsync(_buffId, CancellationToken.None);
            if (txtReq != null)
                txtReq.text = preview.CanUpgrade
                    ? $"Upgrade: {preview.RequiredShards} shards + {preview.RequiredArenaToken} token → Lv.{preview.NextLevel}"
                    : (preview.Reason ?? "Cannot upgrade.");
            if (txtCurrent != null) txtCurrent.text = "Current: " + string.Join(", ", preview.CurrentValues);
            if (txtNext != null) txtNext.text = "Next: " + string.Join(", ", preview.NextValues);
            if (btnUpgrade != null) btnUpgrade.interactable = preview.CanUpgrade;
        }

        private async UniTaskVoid UpgradeAsync()
        {
            var facade = PvpManager.Instance?.Facade;
            if (facade?.Progression == null || string.IsNullOrEmpty(_buffId)) return;

            string txId = $"upgrade-{_buffId}-{Guid.NewGuid():N}";
            try
            {
                var result = await facade.Progression.UpgradeAsync(txId, _buffId, CancellationToken.None);
                UIManager.Instance.ShowToast(result.Message);
                await RefreshAsync();
            }
            catch (Exception e) { UIManager.Instance.ShowToast($"Upgrade failed: {e.Message}"); }
        }
    }
}
