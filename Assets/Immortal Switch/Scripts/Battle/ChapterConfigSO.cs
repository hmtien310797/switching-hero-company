using System;
using UnityEngine;

namespace Immortal_Switch.Scripts.Level.Stage
{
    [CreateAssetMenu(fileName = "ChapterConfig", menuName = "ScriptableObjects/Stage/ChapterConfig")]
    public class ChapterConfigSO : ScriptableObject
    {
        public ChapterConfig[] Chapters;
    }

    [Serializable]
    public class ChapterConfig
    {
        public int ChapterId;
        public string ChapterName;
        [Min(1)] public int StageCount = 20;

        public Element ChapterElement;

        public string RewardRuleId;
        public string EnemyPatternRuleId;
        public string BossPatternRuleId;
        public string ElementRuleId;

        [Min(0f)] public float AfkRewardMultiplier = 1f;
    }
}