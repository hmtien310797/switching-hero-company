using System;
using Battle;
using UnityEngine;

namespace Immortal_Switch.Scripts.Level.Stage
{
    [CreateAssetMenu(fileName = "BossPatternRule", menuName = "ScriptableObjects/Stage/BossPatternRule")]
    public class BossPatternRuleSO : ScriptableObject
    {
        public BossPatternRule[] Rules;
    }

    [Serializable]
    public class BossPatternRule
    {
        public string RuleId;
        public Element RequiredElement;

        [Min(1)]
        public int StagesPerBoss = 1;

        public int[] BossLoopIds;
    }
}