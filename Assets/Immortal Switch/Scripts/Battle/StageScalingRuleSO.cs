using System;
using UnityEngine;

namespace Immortal_Switch.Scripts.Level.Stage
{
    [CreateAssetMenu(fileName = "StageScalingRule", menuName = "ScriptableObjects/Stage/StageScalingRule")]
    public class StageScalingRuleSO : ScriptableObject
    {
        public StageScalingRule[] Rules;
    }

    [Serializable]
    public class StageScalingRule
    {
        public string ScalingRuleId;

        public string EnemyHpMultiplierFormula;
        public string EnemyAtkMultiplierFormula;
        public string EnemyDefMultiplierFormula;

        public string BossHpMultiplierFormula;
        public string BossAtkMultiplierFormula;
        public string BossDefMultiplierFormula;

        [TextArea]
        public string Note;
    }
}