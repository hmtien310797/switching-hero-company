using UnityEngine;

namespace Immortal_Switch.Scripts.Combat
{
    public static class ElementDamageResolver
    {
        public static ElementRelation GetRelation(Element attackerElement, Element defenderElement)
        {
            if (attackerElement == defenderElement)
                return ElementRelation.Same;

            if (Counters(attackerElement, defenderElement))
                return ElementRelation.Advantage;

            if (Counters(defenderElement, attackerElement))
                return ElementRelation.Disadvantage;

            return ElementRelation.Neutral;
        }

        public static float GetDamageMultiplier(
            Element attackerElement,
            Element defenderElement,
            ElementDamageRule rule
        )
        {
            if (rule == null)
                return 1f;

            ElementRelation relation = GetRelation(attackerElement, defenderElement);

            float modifier = relation switch
            {
                ElementRelation.Advantage => rule.AdvantageDamageBonus,
                ElementRelation.Disadvantage => rule.DisadvantageDamagePenalty,
                ElementRelation.Same => rule.EnableSameElementModifier ? rule.SameElementDamageModifier : 0f,
                ElementRelation.Neutral => rule.EnableNeutralModifier ? rule.NeutralDamageModifier : 0f,
                _ => 0f
            };

            return Mathf.Max(0f, 1f + modifier);
        }

        private static bool Counters(Element attacker, Element defender)
        {
            // Ngũ hành tương khắc:
            // Metal/Kim  -> Wood/Mộc
            // Wood/Mộc   -> Earth/Thổ
            // Earth/Thổ  -> Water/Thủy
            // Water/Thủy -> Fire/Hỏa
            // Fire/Hỏa   -> Metal/Kim

            return
                attacker == Element.Metal && defender == Element.Wood ||
                attacker == Element.Wood && defender == Element.Earth ||
                attacker == Element.Earth && defender == Element.Water ||
                attacker == Element.Water && defender == Element.Fire ||
                attacker == Element.Fire && defender == Element.Metal;
        }
    }
}