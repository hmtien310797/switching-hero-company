using System;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Data
{
    /// <summary>
    /// 1 hiệu ứng stat của 1 Formation Buff theo level (DOCX §11 "Buff Detail", §28 "Upgrade").
    /// Level L → <see cref="BaseValue"/> + <see cref="ValuePerLevel"/> * (L-1).
    /// Dùng <see cref="StatType"/> + <see cref="ModifierOp"/> hiện có (DOCX §19).
    /// </summary>
    [Serializable]
    public sealed class FormationBuffStatEffect
    {
        public StatType StatType;
        public ModifierOp Operation = ModifierOp.Add;

        [Tooltip("Giá trị ở Level 1.")] public float BaseValue;

        [Tooltip("Cộng thêm mỗi level kế tiếp. Level L = Base + ValuePerLevel*(L-1).")]
        public float ValuePerLevel;

        public float GetValue(int level)
        {
            int lvl = Mathf.Max(1, level);
            return BaseValue + ValuePerLevel * (lvl - 1);
        }
    }
}
