using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.TransmutationSystem
{
    /// <summary>
    /// Mapping modifier -> stat info.
    /// </summary>
    public class ModifierStatMapping
    {
        /// <summary>
        /// Stat type tương ứng.
        /// </summary>
        public StatType StatType { get; set; }

        /// <summary>
        /// Loại operation áp dụng stat.
        /// Add      = cộng thẳng.
        /// Multiply = cộng theo phần trăm.
        /// </summary>
        public ModifierOp Op { get; set; }
    }
}