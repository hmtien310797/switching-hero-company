using Immortal_Switch.Scripts.Level.Stage;

namespace Immortal_Switch.Scripts.StageSelection
{
    public class StageSelectionPreviewData
    {
        public StageRuntimeData RuntimeData;

        public int GlobalStage => RuntimeData != null ? RuntimeData.GlobalStage : 0;
        public int ChapterId => RuntimeData != null ? RuntimeData.ChapterId : 0;
        public string ChapterName => RuntimeData != null ? RuntimeData.ChapterName : string.Empty;
        public int LocalStage => RuntimeData != null ? RuntimeData.LocalStage : 0;

        public StageReward[] BaseRewards => RuntimeData != null ? RuntimeData.BaseRewards : null;
        public StageReward[] ClearRewards => RuntimeData != null ? RuntimeData.ClearRewards : null;

        public int[] EnemyIds => RuntimeData != null ? RuntimeData.EnemyIds : null;
        public float[] EnemyRates => RuntimeData != null ? RuntimeData.EnemyRates : null;

        public int BossId => RuntimeData != null ? RuntimeData.BossId : 0;

        public bool IsValid => RuntimeData != null && RuntimeData.GlobalStage > 0;
    }
}