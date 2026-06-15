using System;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Common;
using Immortal_Switch.Scripts.Level.Stage;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.StageSelection
{
    public class StageTowerScrollView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private StageNodeItemView itemPrefab;

        [Header("Scroll")]
        [SerializeField] private bool scrollToSelectedOnBind = true;
        [SerializeField] private float scrollToSelectedDelay = 0.05f;

        private int selectedStage;
        private int currentBattleStage;
        private int highestUnlockedStage;

        private Func<int, StageRuntimeData> resolveStageFunc;
        private Action<int> onStageClicked;

        public async UniTask Bind(
            int selectedStage,
            int currentBattleStage,
            int highestUnlockedStage,
            int chapterStartStage,
            int chapterEndStage,
            Func<int, StageRuntimeData> resolveStageFunc,
            Action<int> onStageClicked
        )
        {
            this.selectedStage = selectedStage;
            this.currentBattleStage = currentBattleStage;
            this.highestUnlockedStage = highestUnlockedStage;
            this.resolveStageFunc = resolveStageFunc;
            this.onStageClicked = onStageClicked;

            Clear();

            if (contentRoot == null || itemPrefab == null)
                return;

            // Stage cao nằm trên, stage thấp nằm dưới.
            for (int stage = chapterEndStage; stage >= chapterStartStage; stage--)
            {
                StageRuntimeData data = resolveStageFunc?.Invoke(stage);
                if (data == null)
                    continue;

                StageNodeItemView item = Instantiate(itemPrefab, contentRoot);

                bool isSelected = stage == selectedStage;
                bool isCurrentStage = stage == currentBattleStage;
                bool isLocked = stage > highestUnlockedStage;

                Sprite icon = await GetStageIcon(data);

                item.Bind(
                    stage,
                    icon,
                    isSelected,
                    isLocked,
                    isCurrentStage,
                    HandleStageClicked
                );
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

            if (scrollToSelectedOnBind)
                Invoke(nameof(ScrollToSelectedStage), scrollToSelectedDelay);
        }

        public void RefreshSelection(
            int selectedStage,
            int currentBattleStage,
            int highestUnlockedStage,
            int chapterStartStage,
            int chapterEndStage
        )
        {
            // Đơn giản nhất: rebuild lại.
            // Chapter chỉ 20/30/50 stage nên không đáng kể.
            Bind(
                selectedStage,
                currentBattleStage,
                highestUnlockedStage,
                chapterStartStage,
                chapterEndStage,
                resolveStageFunc,
                onStageClicked
            ).Forget();
        }

        private void HandleStageClicked(int stage)
        {
            onStageClicked?.Invoke(stage);
        }

        private async UniTask<Sprite> GetStageIcon(StageRuntimeData data)
        {
            if (data.BossId > 0)
            {
                Sprite bossIcon = null;

                if (MasterDataCache.Instance.TryGetBossData(data.BossId, out BossDataSO bossDataSo))
                {
                    bossIcon = await AddressableSpawnService.LoadSpriteAsync(bossDataSo.IconKey);
                }
                
                return bossIcon;
            }

            if (data.EnemyIds != null && data.EnemyIds.Length > 0)
            {
                if (MasterDataCache.Instance.TryGetCreepData(data.BossId, out CreepDataSo creepDataSo))
                {
                    return await AddressableSpawnService.LoadSpriteAsync(creepDataSo.IconKey);
                }
            }

            return null;
        }

        private void ScrollToSelectedStage()
        {
            if (scrollRect == null || contentRoot == null)
                return;

            int childCount = contentRoot.childCount;
            if (childCount <= 1)
                return;

            int selectedIndex = -1;

            for (int i = 0; i < childCount; i++)
            {
                StageNodeItemView item = contentRoot.GetChild(i).GetComponent<StageNodeItemView>();
                if (item == null)
                    continue;

                if (item.Stage == selectedStage)
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (selectedIndex < 0)
                return;

            // Vì list đang render chapterEnd -> chapterStart:
            // index 0 ở top, index cuối ở bottom.
            float normalized = 1f - (selectedIndex / (float)(childCount - 1));
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalized);
        }

        private void Clear()
        {
            CancelInvoke(nameof(ScrollToSelectedStage));

            if (contentRoot == null)
                return;

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
        }
    }
}