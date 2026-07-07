using System;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Common;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.Shared;
using UnityEngine;

namespace Immortal_Switch.Scripts.StageSelection
{
    public class StageTowerListView : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        [SerializeField] private StageNodeItemView itemPrefab;

        [Header("Config")]
        [SerializeField] private int visibleNodeCount = 5;

        public async UniTask Bind(
            int viewCenterStage,
            int selectedStage,
            int currentBattleStage,
            int highestUnlockedStage,
            int chapterStartStage,
            int chapterEndStage,
            Func<int, StageRuntimeData> resolveStageFunc,
            Action<int> onStageClicked
        )
        {
            Clear();

            if (contentRoot == null || itemPrefab == null)
                return;

            int count = Mathf.Max(1, visibleNodeCount);
            int half = count / 2;

            int start = viewCenterStage + half;
            int end = viewCenterStage - half;

            for (int stage = start; stage >= end; stage--)
            {
                if (stage < chapterStartStage || stage > chapterEndStage)
                    continue;

                StageRuntimeData data = resolveStageFunc?.Invoke(stage);
                if (data == null)
                    continue;

                StageNodeItemView item = Instantiate(itemPrefab, contentRoot);

                bool isSelected = stage == selectedStage;
                bool isLocked = stage > highestUnlockedStage;
                bool isCurrentStage = stage == currentBattleStage;
                var isViewportBelowCurrentStage = stage < highestUnlockedStage && !isSelected;

                Sprite icon = await GetStageIcon(data);

                item.Bind(
                    stage,
                    icon,
                    isSelected,
                    isLocked,
                    isCurrentStage,
                    isViewportBelowCurrentStage,
                    onStageClicked
                );
            }
        }

        private async UniTask<Sprite> GetStageIcon(StageRuntimeData data)
        {
            if (data.BossId > 0)
            {
                Sprite bossIcon = null;
                if (DatabaseManager.Instance.TryGetBossData(data.BossId, out BossDataSO bossDataSo))
                {
                    bossIcon = bossDataSo.Icon;
                }
                if (bossIcon != null)
                    return bossIcon;
            }

            if (data.EnemyIds != null && data.EnemyIds.Length > 0)
            {
                Sprite creepIcon = null;
                if (DatabaseManager.Instance.TryGetCreepData(data.EnemyIds[0], out CreepDataSo creepData))
                {
                    creepIcon = await AddressableSpawnService.LoadSpriteAsync(creepData.IconKey);
                }
                return creepIcon;
            }

            return null;
        }

        private void Clear()
        {
            if (contentRoot == null)
                return;

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
        }
    }
}