using System;
using UnityEngine;

namespace Immortal_Switch.Scripts.Combat
{
    [CreateAssetMenu(fileName = "ElementRule", menuName = "ScriptableObjects/Combat/ElementRule")]
    public class ElementRuleSO : ScriptableObject
    {
        public ElementDamageRule[] Rules;
    }

    [Serializable]
    public class ElementDamageRule
    {
        public string ElementRuleId;

        [Tooltip("Hero/attacker khắc enemy/defender. Ví dụ Earth đánh Water.")]
        public float AdvantageDamageBonus = 0.3f;

        [Tooltip("Hero/attacker bị enemy/defender khắc. Ví dụ Fire đánh Water.")]
        public float DisadvantageDamagePenalty = -0.2f;

        [Tooltip("Cùng hệ. Thường để 0.")]
        public float SameElementDamageModifier = 0f;

        [Tooltip("Không có quan hệ khắc. Thường để 0.")]
        public float NeutralDamageModifier = 0f;

        public bool EnableSameElementModifier = true;
        public bool EnableNeutralModifier = true;
    }

    public enum ElementRelation
    {
        Neutral = 0,
        Same = 1,
        Advantage = 2,
        Disadvantage = 3
    }
}