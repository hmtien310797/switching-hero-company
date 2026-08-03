using System;

namespace Immortal_Switch.Scripts.Pvp.Mock
{
    /// <summary>
    /// Tạo RandomSeed (ulong) cho BattleSnapshot (DOCX §10, §14 — seeded RNG). PvPBattleRandomService
    /// khởi tạo từ seed này. Phase-1 client tạo; Phase-2 server có thể cấp seed riêng.
    /// </summary>
    public static class RandomSeedFactory
    {
        public static ulong Create()
        {
            // Guid → 8 bytes → ulong. Đủ ngẫu nhiên cho Phase-1 local.
            byte[] bytes = Guid.NewGuid().ToByteArray();
            ulong lo = (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
            ulong hi = (uint)(bytes[4] | (bytes[5] << 8) | (bytes[6] << 16) | (bytes[7] << 24));
            return (hi << 32) | lo;
        }

        /// <summary>Stable seed (ulong) từ 1 string id — dùng cho mock opponent generator (DOCX §30).</summary>
        public static ulong StableSeed(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0xC0FFEEUL;
            ulong hash = 1469598103934665603UL;   // FNV offset
            foreach (byte b in id)
            {
                hash ^= b;
                hash *= 1099511628211UL;           // FNV prime
            }
            return hash;
        }
    }
}
