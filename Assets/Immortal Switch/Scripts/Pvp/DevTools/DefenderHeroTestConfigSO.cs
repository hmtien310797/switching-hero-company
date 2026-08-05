using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.DevTools
{
    /// <summary>
    /// Config để giả lập defender hero trong PvP hero-vs-hero như một user khác (DOCX §30 phase-1
    /// local mock). Chỉ build snapshot từ config này khi <see cref="Enabled"/>==true; ngược lại
    /// matchmaking dùng mock opponent random như cũ. Tool "Defender Hero Test" (EditorWindow) ghi
    /// asset này vào Assets/Resources để runtime load qua <c>Resources.Load</c>.
    /// <para>Growth/Transmutation (powerup) tạm bỏ qua.</para>
    /// </summary>
    [CreateAssetMenu(fileName = "DefenderHeroTest", menuName = "PvP/Defender Hero Test Config")]
    public sealed class DefenderHeroTestConfigSO : ScriptableObject
    {
        public const string ResourcePath = "DefenderHeroTest";

        public bool Enabled;

        [Header("Defender Front Hero")]
        public DefenderSlotConfig Front;

        [Header("Defender Back Hero")]
        public DefenderSlotConfig Back;

        public static DefenderHeroTestConfigSO LoadOrCreate()
        {
            var cfg = Resources.Load<DefenderHeroTestConfigSO>(ResourcePath);
            return cfg != null && cfg.Front != null && cfg.Back != null ? cfg : null;
        }
    }

    [Serializable]
    public sealed class DefenderSlotConfig
    {
        public int HeroId;
        public HeroProgressTier Tier = HeroProgressTier.Legendary;
        public int Star = 5;
        public int Level = 1;

        [Header("Class Skills (up to 5)")]
        public List<SkillEntry> Skills = new();

        [Header("Standard Weapon")]
        public int StandardWeaponId;
        public int StandardWeaponLevel = 1;

        [Header("Exclusive Weapon")]
        public int ExclusiveWeaponId;
        public int ExclusiveWeaponLevel = 1;
        public int ExclusiveWeaponStar = 1;
        public bool UseExclusive;
    }

    [Serializable]
    public sealed class SkillEntry
    {
        public int SkillId;
        public int SkillLevel = 1;
    }
}