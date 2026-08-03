using System;
using System.Collections.Generic;
using Common;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    /// <summary>
    /// Build <see cref="HeroBattleSnapshot"/> (DOCX §9). FinalStats = combat input trực tiếp (đọc
    /// StatsController). Progression fragments qua adapter (đọc live managers cho player).
    /// <see cref="BuildForPlayer"/> = convenience cho player; <see cref="Build"/> = agnostic core
    /// (mock generator dùng ở M4). Sau khi snapshot tạo, combat không đọc UserDataCache/ES3 (DOCX §10).
    /// </summary>
    public static class HeroBattleSnapshotBuilder
    {
        private static readonly List<IHeroPowerSourceAdapter> AdditionalSourceAdapters = new();

        public static void RegisterAdditionalSource(IHeroPowerSourceAdapter adapter)
        {
            if (adapter != null && !AdditionalSourceAdapters.Contains(adapter))
                AdditionalSourceAdapters.Add(adapter);
        }

        /// <summary>
        /// Build cho player hero: đọc live managers (progression/equipment/skills/growth/transmutation)
        /// + HeroActor.Stats (đã có mọi modifier). <paramref name="inventory"/> để resolve buff level.
        /// </summary>
        public static HeroBattleSnapshot BuildForPlayer(HeroActor hero, FormationSlot slot,
            PvpBuffSlotLoadout loadout, IPvPBuffInventoryService inventory, int heroDataVersion = 1)
        {
            if (hero == null) return null;

            int heroId = hero.GetHeroId();
            var progression = HeroProgressionManager.Instance?.Service?.GetCurrentStats(heroId);
            var heroInstance = FindHeroInstance(heroId);

            var ctx = new HeroSnapshotBuildContext
            {
                HeroId = heroId,
                AssignedSlot = slot,
                HeroDataVersion = heroDataVersion,
                Level = heroInstance?.Level ?? 1,
                Star = progression != null ? progression.CurrentStarInTier : (heroInstance?.Star ?? 0),
                Tier = progression != null ? progression.CurrentTier : HeroProgressTier.Common,
                Stats = hero.Stats,
                HeroData = hero.HeroData,
                FormationBuffs = ResolveFormationBuffs(loadout, slot, inventory),
                Equipment = EquipmentSnapshotAdapter.Build(heroId),
                Skills = SkillProgressionSnapshotAdapter.Build(heroId),
                Growth = GrowthSnapshotAdapter.Build(),
                Transmutation = TransmutationSnapshotAdapter.Build(),
                AdditionalPowerSources = CollectAdditionalSources(heroId),
                PassiveEffects = new List<BattlePassiveEffectSnapshot>()
            };

            return Build(ctx);
        }

        /// <summary>Agnostic core: assemble HeroBattleSnapshot từ context + compute FinalStats.</summary>
        public static HeroBattleSnapshot Build(HeroSnapshotBuildContext ctx)
        {
            if (ctx == null) return null;

            return new HeroBattleSnapshot
            {
                HeroId = ctx.HeroId,
                HeroDataVersion = ctx.HeroDataVersion,
                AssignedSlot = ctx.AssignedSlot,
                Level = ctx.Level,
                Star = ctx.Star,
                Tier = ctx.Tier,
                Equipment = ctx.Equipment,
                Skills = ctx.Skills,
                Growth = ctx.Growth,
                Transmutation = ctx.Transmutation,
                AdditionalPowerSources = ctx.AdditionalPowerSources ?? new List<HeroPowerSourceSnapshot>(),
                FinalStats = ResolveFinalStats(ctx),
                PassiveEffects = ctx.PassiveEffects ?? new List<BattlePassiveEffectSnapshot>(),
                FormationBuffs = ctx.FormationBuffs ?? new List<FormationBuffSnapshot>()
            };
        }

        private static RuntimeStatSnapshot ResolveFinalStats(HeroSnapshotBuildContext ctx)
        {
            if (ctx == null) return new RuntimeStatSnapshot();
            // Player path: live StatsController (đã có mọi modifier). Mock path: PrebuiltFinalStats.
            if (ctx.Stats != null) return BuildFinalStats(ctx.Stats);
            return ctx.PrebuiltFinalStats ?? new RuntimeStatSnapshot();
        }

        private static RuntimeStatSnapshot BuildFinalStats(StatsController stats)
        {
            var snap = new RuntimeStatSnapshot();
            if (stats?.StatModule == null) return snap;

            foreach (StatType st in Enum.GetValues(typeof(StatType)))
                snap.Set(st, stats.StatModule.GetFinalStat(st));

            return snap;
        }

        private static List<FormationBuffSnapshot> ResolveFormationBuffs(
            PvpBuffSlotLoadout loadout, FormationSlot slot, IPvPBuffInventoryService inventory)
        {
            var list = new List<FormationBuffSnapshot>();
            if (loadout == null || inventory == null) return list;

            AppendBuff(list, loadout.Core, BuffSlotType.Core, slot, inventory);
            AppendBuff(list, loadout.Support, BuffSlotType.Support, slot, inventory);
            AppendBuff(list, loadout.Trigger, BuffSlotType.Trigger, slot, inventory);
            return list;
        }

        private static void AppendBuff(List<FormationBuffSnapshot> list, string buffId,
            BuffSlotType slotType, FormationSlot slot, IPvPBuffInventoryService inventory)
        {
            if (string.IsNullOrEmpty(buffId)) return;

            var owned = inventory.GetOwnedBuff(buffId);
            list.Add(new FormationBuffSnapshot
            {
                BuffId = buffId,
                Level = owned != null && owned.Level > 0 ? owned.Level : 1,
                BuffDataVersion = 1,
                Slot = slot,
                SlotType = slotType
            });
        }

        private static List<HeroPowerSourceSnapshot> CollectAdditionalSources(int heroId)
        {
            var list = new List<HeroPowerSourceSnapshot>();
            foreach (var adapter in AdditionalSourceAdapters)
            {
                var src = adapter.Build(heroId);
                if (src != null) list.Add(src);
            }
            return list;
        }

        private static HeroInstance FindHeroInstance(int heroId)
        {
            var owned = UserDataCache.Instance?.HeroList?.Owned;
            if (owned == null) return null;
            foreach (var h in owned)
                if (h != null && h.HeroId == heroId) return h;
            return null;
        }
    }
}
