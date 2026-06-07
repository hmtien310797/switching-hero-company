using Battle;
using Immortal_Switch.Scripts.Level.Stage;
using UnityEngine;

namespace Immortal_Switch.Scripts.Level.Stage
{
    public class StageRuntimeData
    {
        public int GlobalStage;
        public int ChapterIndex;
        public int ChapterId;
        public string ChapterName;
        public int LocalStage;
        public Element ChapterElement;

        public string EnemyPatternRuleId;
        public string EnemyPatternId;
        public int[] EnemyIds;
        public float[] EnemyRates;

        public string BossPatternRuleId;
        public int BossId;

        public float AfkRewardMultiplier;
        
        public StageReward[] BaseRewards;
        public StageReward[] ClearRewards;

        public StageStatScale EnemyScale;
        public StageStatScale BossScale;

        public bool IsValid =>
            GlobalStage > 0 &&
            EnemyIds != null &&
            EnemyIds.Length > 0 &&
            EnemyRates != null &&
            EnemyRates.Length == EnemyIds.Length &&
            BossId > 0;
    }
}