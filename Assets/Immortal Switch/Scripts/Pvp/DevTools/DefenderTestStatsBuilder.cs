using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.Models;
using Immortal_Switch.Scripts.Equipment.Services;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Snapshot;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.DevTools
{
    /// <summary>
    /// Build <see cref="HeroBattleSnapshot"/> cho defender từ <see cref="DefenderSlotConfig"/>.
    /// Mirror pipeline của player (HeroProgressionService.GetCurrentStats + WeaponStatBuilder) nhưng
    /// không đụng save data — dùng tier/star/level + equipment trong config. FinalStats là combat
    /// input trực tiếp; Skills/Equipment fragments giữ cho runtime áp dụng skill/level.
    /// </summary>
    public static class DefenderTestStatsBuilder
    {
        public static HeroBattleSnapshot BuildHeroSnapshot(DefenderSlotConfig slot, FormationSlot assignedSlot)
        {
            if (slot == null || slot.HeroId <= 0) return null;

            var heroData = DatabaseManager.Instance?.GetHeroDataById(slot.HeroId);
            if (heroData == null)
            {
                Debug.LogWarning($"[PvP] DefenderTestStatsBuilder: HeroDataSO not found for heroId={slot.HeroId}.");
                return null;
            }

            var ctx = new HeroSnapshotBuildContext
            {
                HeroId = slot.HeroId,
                AssignedSlot = assignedSlot,
                HeroDataVersion = 1,
                Level = Mathf.Max(1, slot.Level),
                Star = Mathf.Max(0, slot.Star),
                Tier = slot.Tier,
                Skills = BuildSkills(slot),
                Equipment = BuildEquipment(slot),
                PrebuiltFinalStats = BuildFinalStats(slot, heroData)
            };

            return HeroBattleSnapshotBuilder.Build(ctx);
        }

        /// <summary>
        /// Base stat (progression node + HeroDataSO), KHÔNG có weapon. Dùng để set base cho defender
        /// actor, rồi apply weapon modifiers riêng (có sourceId) — để StatsController hiện breakdown
        /// nguồn giống attacker. <see cref="BuildFinalStats"/> = base + weapon (cho snapshot/sim).
        /// </summary>
        public static RuntimeStatSnapshot BuildBaseStats(DefenderSlotConfig slot, HeroDataSO heroData)
        {
            var snap = new RuntimeStatSnapshot();

            var node = GetProgressionNode(slot);
            float maxHp = node != null && node.HealthMultiplier != 0f ? node.HealthMultiplier : heroData.Health;
            float atk = node != null && node.AttackMultiplier != 0f ? node.AttackMultiplier : heroData.Attack;
            float def = node != null && node.DefenseMultiplier != 0f ? node.DefenseMultiplier : heroData.Defense;

            snap.Set(StatType.MaxHp, maxHp);
            snap.Set(StatType.Atk, atk);
            snap.Set(StatType.Def, def);
            snap.Set(StatType.AttackRange, heroData.AttackRange);
            snap.Set(StatType.AttackSpeed, heroData.AttackSpeed);
            snap.Set(StatType.CritChance, heroData.CritChance);
            snap.Set(StatType.CritDamage, heroData.CritDamage);
            snap.Set(StatType.Accuracy, heroData.Accuracy);
            return snap;
        }

        /// <summary>Full final stats (base + weapon) — dùng cho snapshot/sim/verification.</summary>
        private static RuntimeStatSnapshot BuildFinalStats(DefenderSlotConfig slot, HeroDataSO heroData)
        {
            var snap = BuildBaseStats(slot, heroData);

            // Weapon modifiers (WeaponStatBuilder — same math as HeroEquipmentRuntimeBridge).
            List<StatModifier> modifiers = BuildWeaponModifiers(slot.HeroId, BuildEquipment(slot));
            for (int i = 0; i < modifiers.Count; i++)
            {
                var m = modifiers[i];
                switch (m.StatType)
                {
                    case StatType.MaxHp: snap.Set(StatType.MaxHp, Apply(snap.Get(StatType.MaxHp), m)); break;
                    case StatType.Atk: snap.Set(StatType.Atk, Apply(snap.Get(StatType.Atk), m)); break;
                    case StatType.Def: snap.Set(StatType.Def, Apply(snap.Get(StatType.Def), m)); break;
                    case StatType.AttackRange: snap.Set(StatType.AttackRange, Apply(snap.Get(StatType.AttackRange), m)); break;
                    case StatType.AttackSpeed: snap.Set(StatType.AttackSpeed, Apply(snap.Get(StatType.AttackSpeed), m)); break;
                    case StatType.CritChance: snap.Set(StatType.CritChance, Apply(snap.Get(StatType.CritChance), m)); break;
                    case StatType.CritDamage: snap.Set(StatType.CritDamage, Apply(snap.Get(StatType.CritDamage), m)); break;
                    case StatType.Accuracy: snap.Set(StatType.Accuracy, Apply(snap.Get(StatType.Accuracy), m)); break;
                    default: snap.Set(m.StatType, Apply(0f, m)); break; // Penetration/LifeSteal/... base 0
                }
            }

            // [DEBUG] weapon từng áp được hay không.
            var weaponDb = DatabaseManager.Instance?.GetWeaponDatabase();
            Debug.Log($"[PvP][DefenderTest] hero={slot.HeroId} Atk={snap.Get(StatType.Atk)} MaxHp={snap.Get(StatType.MaxHp)} Def={snap.Get(StatType.Def)} | " +
                $"weaponDbNull={(DatabaseManager.Instance == null || weaponDb == null)} " +
                $"weaponId={slot.StandardWeaponId} useEx={slot.UseExclusive} " +
                $"weaponMods={modifiers.Count} tier={slot.Tier} star={slot.Star} nodeNull={GetProgressionNode(slot) == null}");

            return snap;
        }

        private static HeroProgressionNode GetProgressionNode(DefenderSlotConfig slot)
        {
            try
            {
                var db = DatabaseManager.Instance?.HeroProgressionDatabase;
                var config = db != null ? db.GetProgressionConfig(slot.HeroId) : null;
                if (config == null) return null;

                int star = Mathf.Max(0, slot.Star);
                var node = config.GetNode(slot.Tier, star);
                if (node != null) return node;

                // Fallback 1: star ngoài phạm vi node (progression chỉ có StarInTier 0-2) → clamp vô tier.
                int maxStar = config.GetMaxStarInTier(slot.Tier);
                node = config.GetNode(slot.Tier, Mathf.Min(star, maxStar));
                if (node != null) return node;

                // Fallback 2: node cao nhất có sẵn (tier/star config không tồn tại) → tránh défender yếu.
                HeroProgressionNode best = null;
                if (config.Nodes != null)
                {
                    for (int i = 0; i < config.Nodes.Count; i++)
                    {
                        var n = config.Nodes[i];
                        if (n == null) continue;
                        if (best == null || n.Tier > best.Tier
                            || (n.Tier == best.Tier && n.StarInTier > best.StarInTier))
                            best = n;
                    }
                }
                return best;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PvP] DefenderTestStatsBuilder.GetProgressionNode({slot.HeroId}): {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Weapon modifiers dạng StatModifier (có sourceId giống attacker — WeaponRuntimeIds) để
        /// StatsController liệt kê nguồn. Dùng cho defender actor lẫn snapshot. <paramref name="equip"/>
        /// có thể là config slot hoặc EquipmentSnapshot từ snapshot.
        /// </summary>
        public static List<StatModifier> BuildWeaponModifiers(int heroId, EquipmentSnapshot equip)
        {
            var result = new List<StatModifier>();
            var weaponDb = DatabaseManager.Instance?.GetWeaponDatabase();
            if (weaponDb == null || equip == null) return result;

            if (equip.UseExclusive && equip.ExclusiveWeaponId > 0)
            {
                var exc = weaponDb.GetExclusive(equip.ExclusiveWeaponId);
                if (exc != null && exc.HeroId == heroId)
                {
                    result.AddRange(WeaponStatBuilder.BuildForExclusive(exc,
                        new ExclusiveWeaponState
                        {
                            ExclusiveWeaponId = exc.ExclusiveWeaponId,
                            HeroId = heroId,
                            IsUnlocked = true,
                            Level = Mathf.Max(1, equip.ExclusiveWeaponLevel),
                            CurrentStar = Mathf.Max(1, equip.ExclusiveWeaponStar)
                        }, WeaponRuntimeIds.Exclusive(heroId, exc.ExclusiveWeaponId)));
                }
                return result;
            }

            if (equip.StandardWeaponId > 0)
            {
                var std = weaponDb.GetStandard(equip.StandardWeaponId);
                if (std != null)
                {
                    result.AddRange(WeaponStatBuilder.BuildForStandard(std,
                        new StandardWeaponState
                        {
                            WeaponId = std.WeaponId,
                            IsUnlocked = true,
                            Level = Mathf.Max(1, equip.StandardWeaponLevel)
                        }, WeaponRuntimeIds.Standard(heroId, std.WeaponId)));
                }
                else
                {
                    Debug.LogWarning($"[PvP][DefenderTest] GetStandard({equip.StandardWeaponId}) returned null — weapon NOT applied.");
                }
            }
            return result;
        }

        private static float Apply(float scaledBase, StatModifier m)
        {
            // Modifier áp dụng lên giá trị sau multiplier. Giữ đơn giản cho test tool.
            if (m.Operation == ModifierOp.Multiply)
                return scaledBase * Mathf.Max(0f, m.Value);
            return scaledBase + m.Value;
        }

        private static SkillProgressionSnapshot BuildSkills(DefenderSlotConfig slot)
        {
            var snap = new SkillProgressionSnapshot { HeroId = slot.HeroId };
            if (slot.Skills == null) return snap;
            for (int i = 0; i < slot.Skills.Count && snap.Equipped.Count < ClassSkillSlotCount; i++)
            {
                var entry = slot.Skills[i];
                if (entry == null || entry.SkillId <= 0) continue;
                snap.Equipped.Add(new SkillSlotSnapshot
                {
                    SkillId = entry.SkillId,
                    Level = Mathf.Max(1, entry.SkillLevel),
                    Grade = string.Empty
                });
            }
            return snap;
        }

        private static EquipmentSnapshot BuildEquipment(DefenderSlotConfig slot)
        {
            return new EquipmentSnapshot
            {
                HeroId = slot.HeroId,
                StandardWeaponId = slot.StandardWeaponId,
                StandardWeaponLevel = slot.StandardWeaponLevel,
                ExclusiveWeaponId = slot.ExclusiveWeaponId,
                ExclusiveWeaponLevel = slot.ExclusiveWeaponLevel,
                ExclusiveWeaponStar = slot.ExclusiveWeaponStar,
                UseExclusive = slot.UseExclusive
            };
        }

        private const int ClassSkillSlotCount = 5;
    }
}