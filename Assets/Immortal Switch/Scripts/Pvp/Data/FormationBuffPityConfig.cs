using System;
using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Data
{
    /// <summary>Pity config cho 1 pool (DOCX §21, §26 — soft + hard pity).</summary>
    [Serializable]
    public sealed class FormationBuffPityConfig
    {
        public int SoftPityThreshold = 10;       // rolls since Rare+ → boost rare rates.
        public int HardPityThreshold = 30;       // rolls since Legendary → force Legendary.
        public BuffRarity ForceMinimumRarity = BuffRarity.Legendary;
    }
}
