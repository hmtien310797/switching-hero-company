using System;

namespace Immortal_Switch.Scripts.Pvp.Mock
{
    /// <summary>
    /// Tạo local BattleId unique (DOCX §31, §33 — "Unique BattleId"). Phase-1 client tạo; Phase-2
    /// server sẽ cấp. Format: pvp-&lt;guid&gt;-&lt;unix&gt;.
    /// </summary>
    public static class PvpBattleIdFactory
    {
        public static string Create()
        {
            return $"pvp-{Guid.NewGuid():N}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        }
    }
}
