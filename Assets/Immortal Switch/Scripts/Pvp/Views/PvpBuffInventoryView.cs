using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Buff Inventory (Markdown §04 — owned/unowned filter, rarity, level, shard, equip state).
    /// Entry to Gacha (Roll). Tap item → <see cref="PvpBuffDetailView"/>.
    ///
    /// PREFAB (Addressable = "PvpBuffInventoryView", UILayer.Main, PageExclusive):
    ///   TMP_Text: txtDevBadge, txtToken
    ///   Button: btnGacha, btnClose
    ///   Transform: listContainer
    ///   PvpOptionItemView: itemPrefab
    /// </summary>
    public class PvpBuffInventoryView : UIView
    {
        [SerializeField] private TMP_Text txtDevBadge;
        [SerializeField] private TMP_Text txtToken;
        [SerializeField] private Button btnGacha;
        [SerializeField] private Button btnClose;
        [SerializeField] private Transform listContainer;
        [SerializeField] private PvpOptionItemView itemPrefab;

        private void Awake()
        {
            if (btnGacha != null) btnGacha.onClick.AddListener(() => UIManager.Instance.OpenPopupAsync<PvpBuffGachaView>().Forget());
            if (btnClose != null) btnClose.onClick.AddListener(() => UIManager.Instance.Close<PvpBuffInventoryView>());
        }

        public override void OnShow(object args)
        {
            if (txtDevBadge != null) txtDevBadge.text = "LOCAL MOCK";
            Refresh();
        }

        private void Refresh()
        {
            var facade = PvpManager.Instance?.Facade;
            if (facade == null) return;

            var profile = facade.Profile?.GetCurrent();
            if (txtToken != null && profile != null) txtToken.text = $"Arena Token: {profile.ArenaToken}";

            if (listContainer == null || itemPrefab == null) return;
            for (int i = listContainer.childCount - 1; i >= 0; i--)
                Destroy(listContainer.GetChild(i).gameObject);

            var owned = facade.BuffInventory.LoadOwnedBuffs();
            foreach (var b in owned.Buffs)
            {
                if (b == null) continue;
                var item = Instantiate(itemPrefab, listContainer);
                string name = facade.BuffCatalog.TryGet(b.BuffId, out var def) ? def.BuffNameKey : b.BuffId;
                string rarity = def != null ? def.Rarity.ToString() : "?";
                if (item.label != null)
                    item.label.text = $"{name}\n{rarity} • Lv.{b.Level} • Shards {b.Shards}";
                if (item.button != null)
                {
                    string id = b.BuffId;
                    item.button.onClick.AddListener(() =>
                        UIManager.Instance.OpenPopupAsync<PvpBuffDetailView>(id).Forget());
                }
            }
        }
    }
}
