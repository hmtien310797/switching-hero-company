using Immortal_Switch.Scripts.Pvp;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Screen 13 — Battle History (Markdown §13). List local ES3 history (capped retention).
    ///
    /// PREFAB (Addressable = "PvpHistoryView", UILayer.Main, PageExclusive):
    ///   TMP_Text: txtDevBadge
    ///   Button: btnClose
    ///   Transform: listContainer
    ///   PvpOptionItemView: itemPrefab
    /// </summary>
    public class PvpHistoryView : UIView
    {
        [SerializeField] private TMP_Text txtDevBadge;
        [SerializeField] private Button btnClose;
        [SerializeField] private Transform listContainer;
        [SerializeField] private PvpOptionItemView itemPrefab;

        private void Awake()
        {
            if (btnClose != null) btnClose.onClick.AddListener(() => UIManager.Instance.Close<PvpHistoryView>());
        }

        public override void OnShow(object args)
        {
            if (txtDevBadge != null) txtDevBadge.text = "LOCAL MOCK";
            Refresh();
        }

        private void Refresh()
        {
            var facade = PvpManager.Instance?.Facade;
            if (facade == null || listContainer == null || itemPrefab == null) return;

            for (int i = listContainer.childCount - 1; i >= 0; i--)
                Destroy(listContainer.GetChild(i).gameObject);

            var history = facade.History.LoadHistory();
            foreach (var e in history.Entries)
            {
                if (e == null) continue;
                var item = Instantiate(itemPrefab, listContainer);
                if (item.label != null)
                    item.label.text = $"{e.Result} vs {e.OpponentName}\n{(e.RankChange >= 0 ? "+" : "")}{e.RankChange} rank • {e.DurationMs}ms • Seed {e.RandomSeed}";
                if (item.button != null)
                {
                    string battleId = e.BattleId;
                    item.button.onClick.AddListener(() => UIManager.Instance.ShowToast($"BattleId: {battleId}"));
                }
            }
        }
    }
}
