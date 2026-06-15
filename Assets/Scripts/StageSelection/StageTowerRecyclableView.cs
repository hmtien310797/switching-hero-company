using System;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Common;
using Immortal_Switch.Scripts.Level.Stage;
using RecyclableScrollRect;
using UnityEngine;

namespace Immortal_Switch.Scripts.StageSelection
{
    public class StageTowerRecyclableView : MonoBehaviour, IRSRDataSource
    {
        [Header("RSR")] [SerializeField] private RSR rsr;
        [SerializeField] private GameObject itemPrototype;

        [Header("Item Size")] [SerializeField] private bool isItemSizeKnown = true;
        [SerializeField] private float itemSize = 130f;

        [Header("Scroll")] [SerializeField] private bool scrollToSelectedOnBind = true;
        [SerializeField] private float scrollToSelectedTime = 0f;
        [SerializeField] private bool instantScrollToSelected = true;

        private int selectedStage;
        private int currentBattleStage;
        private int highestUnlockedStage;
        private int chapterStartStage;
        private int chapterEndStage;
        
        private int lastChapterStartStage = -1;
        private int lastChapterEndStage = -1;

        private Func<int, StageRuntimeData> resolveStageFunc;
        private Action<int> onStageClicked;

        private bool initialized;
        private bool scrollToStageAfterBind;

        public int ItemsCount => Mathf.Max(0, chapterEndStage - chapterStartStage + 1);

        public GameObject[] PrototypeItems => itemPrototype != null
            ? new[] { itemPrototype }
            : Array.Empty<GameObject>();

        public bool IsItemSizeKnown => isItemSizeKnown;

        private void Awake()
        {
            if (rsr == null)
                rsr = GetComponent<RSR>();
        }

        public void Bind(
            int selectedStage,
            int currentBattleStage,
            int highestUnlockedStage,
            int chapterStartStage,
            int chapterEndStage,
            Func<int, StageRuntimeData> resolveStageFunc,
            Action<int> onStageClicked,
            bool scrollToStageAfterBind
        )
        {
            bool chapterChanged =
                lastChapterStartStage != chapterStartStage ||
                lastChapterEndStage != chapterEndStage;

            this.selectedStage = selectedStage;
            this.currentBattleStage = currentBattleStage;
            this.highestUnlockedStage = highestUnlockedStage;
            this.chapterStartStage = chapterStartStage;
            this.chapterEndStage = chapterEndStage;
            this.resolveStageFunc = resolveStageFunc;
            this.onStageClicked = onStageClicked;

            lastChapterStartStage = chapterStartStage;
            lastChapterEndStage = chapterEndStage;

            if (rsr == null)
            {
                Debug.LogError("[StageTowerRecyclableView] Missing RSR.");
                return;
            }

            if (itemPrototype == null)
            {
                Debug.LogError("[StageTowerRecyclableView] Missing item prototype.");
                return;
            }

            if (!initialized || !rsr.IsInitialized)
            {
                rsr.Initialize(this);
                initialized = true;
            }
            else
            {
                rsr.ReloadData(chapterChanged);
            }

            ForceRefreshVisibleItems();

            if (scrollToStageAfterBind)
            {
                if (chapterChanged)
                    ScrollToStage(chapterEndStage);
                else
                    ScrollToStage(selectedStage);
            }
        }
        
        private void ForceRefreshVisibleItems()
        {
            if (rsr == null || ItemsCount <= 0)
                return;
            for (int i = 0; i < ItemsCount; i++)
            {
                rsr.ReloadItem(i);
            }
        }

        public void RefreshVisible()
        {
            if (!initialized || rsr == null)
                return;

            rsr.ReloadData(false);
        }

        public void ScrollToStage(int stage)
        {
            if (!initialized || rsr == null)
                return;

            if (ItemsCount <= 0)
                return;

            int index = GetIndexByStage(stage);
            rsr.ScrollToItemIndex(
                index,
                scrollToSelectedTime,
                isSpeed: false,
                instant: instantScrollToSelected,
                callEvent: false
            );
        }

        private int GetStageByIndex(int itemIndex)
        {
            // Render stage cao ở trên, stage thấp ở dưới.
            return chapterEndStage - itemIndex;
        }

        private int GetIndexByStage(int stage)
        {
            return Mathf.Clamp(chapterEndStage - stage, 0, ItemsCount - 1);
        }

        private async UniTask<Sprite> GetStageIcon(StageRuntimeData data)
        {
            if (data.BossId > 0)
            {
                Sprite bossIcon = null;
                if (MasterDataCache.Instance.TryGetBossData(data.BossId, out BossDataSO bossDataSo))
                {
                    bossIcon = bossDataSo.Icon;
                }

                if (bossIcon != null)
                    return bossIcon;
            }

            if (data.EnemyIds != null && data.EnemyIds.Length > 0)
            {
                Sprite creepIcon = null;
                if (MasterDataCache.Instance.TryGetCreepData(data.EnemyIds[0], out CreepDataSo creepData))
                {
                    creepIcon = await AddressableSpawnService.LoadSpriteAsync(creepData.IconKey);
                }

                return creepIcon;
            }

            return null;
        }

        public GameObject GetItemPrototype(int itemIndex)
        {
            return itemPrototype;
        }

        public bool IsItemStatic(int itemIndex)
        {
            return false;
        }

        public void SetItemData(IItem item, int itemIndex)
        {
            SetItemDataAsync(item, itemIndex).Forget();
        }

        private async UniTask SetItemDataAsync(IItem item, int itemIndex)
        {
            if (item is not StageNodeItemView view)
                return;

            int stage = GetStageByIndex(itemIndex);

            StageRuntimeData data = resolveStageFunc?.Invoke(stage);
            if (data == null)
                return;

            bool isSelected = stage == selectedStage;
            bool isCurrentStage = stage == currentBattleStage;
            bool isLocked = stage > highestUnlockedStage;

            Sprite icon = await GetStageIcon(data);

            view.Bind(
                stage,
                icon,
                isSelected,
                isLocked,
                isCurrentStage,
                onStageClicked
            );
        }

        public float GetItemSize(int itemIndex)
        {
            return itemSize;
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
    }
}