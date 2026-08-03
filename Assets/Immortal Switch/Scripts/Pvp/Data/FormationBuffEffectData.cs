using System;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Data
{
    /// <summary>
    /// 1 effect của buff khi trigger (DOCX §18 — "Effect"). Target + effect-specific values.
    /// StatModifier dùng StatType/Operation; Damage/Heal/Shield dùng Amount (scale theo level).
    /// </summary>
    [Serializable]
    public sealed class FormationBuffEffectData
    {
        public FormationBuffEffectType EffectType = FormationBuffEffectType.StatModifier;
        public FormationBuffTargetType Target = FormationBuffTargetType.Owner;

        // StatModifier.
        public StatType StatType;
        public ModifierOp Operation = ModifierOp.Add;
        public float BaseValue;
        public float ValuePerLevel;

        // Damage / Heal / Shield.
        public float AmountBase;
        public float AmountPerLevel;

        public float GetStatValue(int level)
        {
            int lvl = Mathf.Max(1, level);
            return BaseValue + ValuePerLevel * (lvl - 1);
        }

        public float GetAmount(int level)
        {
            int lvl = Mathf.Max(1, level);
            return AmountBase + AmountPerLevel * (lvl - 1);
        }
    }
}
