using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    /// <summary>
    /// FinalStats — combat input trực tiếp (DOCX §9). Snapshot toàn bộ final stat từ StatsController.
    /// Dictionary&lt;StatType,float&gt; — JSON-serializable (Newtonwood round-trip).
    /// </summary>
    [Serializable]
    public sealed class RuntimeStatSnapshot
    {
        public Dictionary<StatType, float> Values = new();

        public float Get(StatType t) => Values != null && Values.TryGetValue(t, out var v) ? v : 0f;

        public void Set(StatType t, float v)
        {
            Values ??= new Dictionary<StatType, float>();
            Values[t] = v;
        }
    }
}
