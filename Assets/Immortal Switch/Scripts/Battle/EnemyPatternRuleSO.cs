using System;
using Battle;
using UnityEngine;

namespace Immortal_Switch.Scripts.Level.Stage
{
    public enum PatternPickMode
    {
        Loop = 0
    }

    [CreateAssetMenu(fileName = "EnemyPatternRule", menuName = "ScriptableObjects/Stage/EnemyPatternRule")]
    public class EnemyPatternRuleSO : ScriptableObject
    {
        public EnemyPatternRule[] Rules;
        public EnemyPatternData[] Patterns;
    }

    [Serializable]
    public class EnemyPatternRule
    {
        public string RuleId;
        public Element RequiredElement;
        public PatternPickMode PickMode = PatternPickMode.Loop;
        public string[] PatternLoopIds;
    }

    [Serializable]
    public class EnemyPatternData
    {
        public string PatternId;
        public Element RequiredElement;

        public int[] EnemyIds;
        public float[] Rates;
    }
}