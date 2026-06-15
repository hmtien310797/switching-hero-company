using System;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Common;
using Immortal_Switch.Scripts.Level.Stage;
using UnityEngine;

namespace Immortal_Switch.Scripts.StageSelection
{
    public class StageTowerRecyclableAdapter : MonoBehaviour
    {
        private int chapterStartStage;
        private int chapterEndStage;
        private int selectedStage;
        private int currentBattleStage;
        private int highestUnlockedStage;

        private Func<int, StageRuntimeData> resolveStageFunc;
        private Action<int> onStageClicked;

        public int ItemCount => Mathf.Max(0, chapterEndStage - chapterStartStage + 1);

        public void SetData(
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
            this.chapterStartStage = chapterStartStage;
            this.chapterEndStage = chapterEndStage;
            this.resolveStageFunc = resolveStageFunc;
            this.onStageClicked = onStageClicked;
        }

        public async UniTask BindCell(StageNodeItemView item, int index)
        {
            if (item == null)
                return;

            int stage = GetStageByIndex(index);
            StageRuntimeData data = resolveStageFunc?.Invoke(stage);

            if (data == null)
            {
                item.gameObject.SetActive(false);
                return;
            }

            item.gameObject.SetActive(true);

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
                onStageClicked
            );
        }

        public int GetIndexByStage(int stage)
        {
            return Mathf.Clamp(chapterEndStage - stage, 0, ItemCount - 1);
        }

        private int GetStageByIndex(int index)
        {
            return chapterEndStage - index;
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
    }
}