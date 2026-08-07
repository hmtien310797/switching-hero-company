using System;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Views.UI;
using RecyclableScrollRect;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// RecyclableScrollRect data source cho danh sách rankings PvP (50 record). Clone pattern của
    /// <c>LeaderboardRankRecyclableView</c>: MonoBehaviour chính là IRSRDataSource, giữ resolver
    /// <c>Func&lt;int, PvpLeaderboardEntryModel&gt;</c> để parent (PvpMainView) làm nguồn dữ liệu.
    /// </summary>
    public class PvpLeaderboardRecyclableView : MonoBehaviour, IRSRDataSource
    {
        [Header("RSR")]
        [SerializeField] private RSR rsr;
        [SerializeField] private GameObject rankPrefab;

        [Header("Item Size")]
        [SerializeField] private bool isItemSizeKnown = true;
        [SerializeField] private float itemSize = 130f;
        [SerializeField] private PvpRankInfoSo pvpRankInfo;

        private Func<int, PvpLeaderboardEntryModel> _onResolveItem;

        public void Bind(int itemCount, Func<int, PvpLeaderboardEntryModel> onResolveItem)
        {
            ItemsCount = itemCount;
            _onResolveItem = onResolveItem;

            // Defensive: nếu prefab chưa wire đủ (rsr hoặc rankPrefab null) thì bỏ qua, không throw
            // (tránh sập luôn top3/countdown/myRank). Log rõ để biết thiếu gì.
            if (rsr == null)
            {
                Debug.LogWarning("[PvP] PvpLeaderboardRecyclableView: rsr chưa assign — bỏ qua list rankings.");
                return;
            }
            if (rankPrefab == null)
            {
                Debug.LogWarning("[PvP] PvpLeaderboardRecyclableView: rankPrefab chưa assign — bỏ qua list rankings.");
                return;
            }

            if (!rsr.IsInitialized)
                rsr.Initialize(this);

            for (var i = 0; i < ItemsCount; i++)
                rsr.ReloadItem(i);
        }

        public int ItemsCount { get; private set; }

        public GameObject[] PrototypeItems =>
            rankPrefab != null ? new[] { rankPrefab } : Array.Empty<GameObject>();

        public GameObject GetItemPrototype(int itemIndex) => rankPrefab;
        public bool IsItemStatic(int itemIndex) => false;
        public bool IsItemSizeKnown => isItemSizeKnown;
        public float GetItemSize(int itemIndex) => itemSize;

        public void SetItemData(IItem item, int itemIndex)
        {
            if (item is not PvpLeaderboardRankItem ui) return;
            var data = _onResolveItem?.Invoke(itemIndex);
            if (data != null)
                ui.Bind(data, pvpRankInfo);
        }

        public void ItemCreated(int itemIndex, IItem item, GameObject itemGo) { }
        public void ItemHidden(IItem item, int itemIndex) { }
        public void ScrolledToItem(IItem item, int itemIndex) { }
        public bool IgnoreContentPadding(int itemIndex) => false;
        public void PullToRefresh() { }
        public void PushToClose() { }
        public void ReachedScrollStart() { }
        public void ReachedScrollEnd() { }
        public void LastItemIsVisible() { }
    }
}