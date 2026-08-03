using System;
using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Data
{
    /// <summary>Tỉ lệ 1 rarity trong pool (DOCX §25 — FormationBuffRarityRate). Rates config-driven.</summary>
    [Serializable]
    public sealed class FormationBuffRarityRate
    {
        public BuffRarity Rarity;
        [UnityEngine.Range(0f, 100f)] public float Rate = 0f;
        public bool Enabled = true;
    }
}
