using System;
using System.Security.Cryptography;
using System.Text;

namespace Immortal_Switch.Scripts.Pvp.Battle
{
    /// <summary>
    /// Hash combat log thành hex SHA256 (DOCX §33 — "CombatLogHash"). Dùng cho Trust-but-Verify
    /// (§36) — ép client produce consistent log; server verify sau.
    /// </summary>
    public static class PvpCombatLogHasher
    {
        public static string Hash(string combatLog)
        {
            string content = combatLog ?? string.Empty;
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }
    }
}
