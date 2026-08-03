using System;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    /// <summary>
    /// Envelope cho future power systems (DOCX §9 — "Future power systems implement an adapter
    /// producing HeroPowerSourceSnapshot"). SourceId + JsonPayload keeps core agnostic của từng
    /// system mới. <b>FLAGGED:</b> generic envelope — specific payload per source.
    /// </summary>
    [Serializable]
    public sealed class HeroPowerSourceSnapshot
    {
        public string SourceId;
        public int DataVersion;
        public string JsonPayload;
    }
}
