using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Battle
{
    /// <summary>
    /// Visual RNG (VFX / particle / camera / sound variation) — tách KHỎI combat RNG (DOCX §14 —
    /// "VFX, particles, camera, and sound variation use a separate visual random service").
    /// Phase-1 được dùng UnityEngine.Random vì visual không affect gameplay outcome.
    /// </summary>
    public static class PvpVisualRandomService
    {
        public static float Range01() => Random.value;

        public static float Range(float min, float max) => Random.Range(min, max);

        public static int RangeInt(int minInclusive, int maxExclusive) =>
            Random.Range(minInclusive, maxExclusive);
    }
}
