using System;
using Common;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Level.Stage;
using UnityEngine;

namespace Immortal_Switch.Scripts.StageSelection
{
    public class StageTowerListView : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        [SerializeField] private StageNodeItemView itemPrefab;

        [Header("Config")]
        [SerializeField] private int visibleNodeCount = 5;

        public void Bind(
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

                Sprite icon = GetStageIcon(data);

                item.Bind(
                    stage,
                    icon,
                    isSelected,
                    isLocked,
                    isCurrentStage,
                    onStageClicked
                );
            }
        }

        private Sprite GetStageIcon(StageRuntimeData data)
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
                    creepIcon = creepData.Icon;
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