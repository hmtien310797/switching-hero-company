using System;

namespace Immortal_Switch.Scripts.Pvp.Battle
{
    /// <summary>
    /// Phase-1 seeded RNG (System.Random) — KHÔNG dùng UnityEngine.Random. Cùng seed + cùng call
    /// order → cùng gameplay sequence (DOCX §14, §41 QA). Combat RNG tách biệt với gacha RNG.
    /// </summary>
    internal sealed class SeededPvPBattleRandomService : Interfaces.IPvPBattleRandomService
    {
        private readonly Random _rng;

        public ulong Seed { get; }

        public SeededPvPBattleRandomService(ulong seed)
        {
            Seed = seed;
            // System.Random lấy int seed — kết hợp high+low để giữ tính ngẫu nhiên tương đối.
            _rng = new Random(unchecked((int)((seed >> 16) ^ (seed & 0x7FFFFFFFL))));
        }

        public float NextFloat()
        {
            // NextDouble() ∈ [0,1). Ép float.
            return (float)_rng.NextDouble();
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return _rng.Next(minInclusive, maxExclusive);
        }

        public bool Roll(float probability)
        {
            return probability > 0f && NextFloat() < probability;
        }
    }
}
