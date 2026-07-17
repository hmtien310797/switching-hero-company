using System;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Leaderboard.Views.UI;
using RecyclableScrollRect;
using UnityEngine;

namespace Immortal_Switch.Scripts.Leaderboard.Views
{
    public class LeaderboardRankResolver
    {
        /// <summary>
        /// rank hien tai
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// ten user
        /// </summary>
        public string PlayerName { get; set; }

        /// <summary>
        /// stage dat duoc
        /// </summary>
        public int Stage { get; set; }

        /// <summary>
        /// reward quantity
        /// </summary>
        public BigNumber RewardQuantity { get; set; }
    }

    public class LeaderboardRankRecyclableView : MonoBehaviour, IRSRDataSource
    {
        [Header("RSR")]
        [SerializeField]
        private RSR rsr;

        [SerializeField]
        private RectTransform rankContainer;

        [SerializeField]
        private GameObject rankPrefab;

        [Header("Item Size")]
        [SerializeField]
        private bool isItemSizeKnown = true;

        [SerializeField]
        private float itemSize = 130f;

        // --- Private Fields ---
        private Func<int, LeaderboardRankResolver> _onResolveItem;

        public void Bind(
            int itemCount,
            Func<int, LeaderboardRankResolver> onResolveItem
        )
        {
            ItemsCount = itemCount;
            _onResolveItem = onResolveItem;

            if (!rsr.IsInitialized)
            {
                rsr.Initialize(this);
            }

            ForceRefreshVisibleItems();
        }

        private void ForceRefreshVisibleItems()
        {
            for (var i = 0; i < ItemsCount; i++)
            {
                rsr.ReloadItem(i);
            }
        }

        public int ItemsCount { get; private set; }

        public GameObject[] PrototypeItems =>
            rankPrefab != null
                ? new[] { rankPrefab, }
                : Array.Empty<GameObject>();

        public GameObject GetItemPrototype(int itemIndex)
        {
            return rankPrefab;
        }

        public bool IsItemStatic(int itemIndex)
        {
            return false;
        }

        public void SetItemData(IItem item, int itemIndex)
        {
            SetItemDataInternal(item, itemIndex);
        }

        public void ItemCreated(int itemIndex, IItem item, GameObject itemGo)
        {
        }

        public void ItemHidden(IItem item, int itemIndex)
        {
        }

        public void ScrolledToItem(IItem item, int itemIndex)
        {
        }

        public bool IgnoreContentPadding(int itemIndex)
        {
            return false;
        }

        public void PullToRefresh()
        {
        }

        public void PushToClose()
        {
        }

        public void ReachedScrollStart()
        {
        }

        public void ReachedScrollEnd()
        {
        }

        public void LastItemIsVisible()
        {
        }

        public bool IsItemSizeKnown => isItemSizeKnown;

        public float GetItemSize(int itemIndex)
        {
            return itemSize;
        }

        private void SetItemDataInternal(IItem item, int itemIndex)
        {
            if (item is not UILeaderboardRankItem ui)
            {
                return;
            }

            var data = _onResolveItem?.Invoke(itemIndex);

            if (data != null)
            {
                ui.Bind(data.Rank, data.PlayerName, data.Stage, false, data.RewardQuantity);
            }
        }
    }
}