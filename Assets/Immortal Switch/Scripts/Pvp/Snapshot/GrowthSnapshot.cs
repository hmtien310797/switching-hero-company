using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    [Serializable]
    public sealed class GrowthStatStackSnapshot
    {
        public StatType Stat;
        public int CurrentStack;
    }

    /// <summary>Growth fragment (DOCX §9). <b>FLAGGED:</b> inferred từ GrowthModels.</summary>
    [Serializable]
    public sealed class GrowthSnapshot
    {
        public int CurrentUnlockedTier;
        public List<GrowthStatStackSnapshot> Stats = new();
    }
}
