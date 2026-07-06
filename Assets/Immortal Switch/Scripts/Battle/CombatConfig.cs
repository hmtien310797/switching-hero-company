using UnityEngine;

namespace Immortal_Switch.Scripts.Combat
{
    public class CombatConfig : MonoBehaviour
    {
        [SerializeField] private ElementRuleSO elementRuleConfig;
        [SerializeField] private string defaultElementRuleId = "default_element_rule";

        private static ElementDamageRule currentElementRule;

        private void Awake()
        {
            Apply();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                Apply();
        }

        public void Apply()
        {
            currentElementRule = FindRule(defaultElementRuleId);
        }

        public static ElementDamageRule CurrentElementRule => currentElementRule;

        private ElementDamageRule FindRule(string ruleId)
        {
            if (elementRuleConfig == null || elementRuleConfig.Rules == null || elementRuleConfig.Rules.Length == 0)
                return null;

            if (!string.IsNullOrWhiteSpace(ruleId))
            {
                for (int i = 0; i < elementRuleConfig.Rules.Length; i++)
                {
                    ElementDamageRule rule = elementRuleConfig.Rules[i];
                    if (rule != null && rule.ElementRuleId == ruleId)
                        return rule;
                }
            }

            return elementRuleConfig.Rules[0];
        }
    }
}