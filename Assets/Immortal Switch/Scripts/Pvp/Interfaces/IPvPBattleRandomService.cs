namespace Immortal_Switch.Scripts.Pvp.Interfaces
{
    /// <summary>
    /// Seeded RNG cho PvP combat (DOCX §14). Khởi tạo từ <c>BattleSnapshot.RandomSeed</c>. Dùng cho
    /// crit / evasion / accuracy-miss / random target / buff chance / AI random. VFX/particle/camera/
    /// sound variation dùng visual random service RIÊNG. Không gọi UnityEngine.Random trực tiếp.
    /// </summary>
    public interface IPvPBattleRandomService
    {
        ulong Seed { get; }

        /// <summary>[0, 1).</summary>
        float NextFloat();

        /// <summary>[minInclusive, maxExclusive).</summary>
        int Range(int minInclusive, int maxExclusive);

        /// <summary>True với <paramref name="probability"/> (0..1).</summary>
        bool Roll(float probability);
    }
}
