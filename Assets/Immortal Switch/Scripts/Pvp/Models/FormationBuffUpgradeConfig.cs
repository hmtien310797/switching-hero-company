using System;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Config upgrade cho 1 level (DOCX §28). "Values are config examples and must not be hard-coded
    /// in UI or service code" — progression service lấy cost từ config/SO, không hard-code.
    /// </summary>
    [Serializable]
    public sealed class FormationBuffUpgradeConfig
    {
        public int TargetLevel;
        public int RequiredShard;
        public long RequiredArenaToken;
    }
}
