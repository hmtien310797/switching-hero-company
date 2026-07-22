using System;
using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Reward;
using UnityEngine;

namespace Immortal_Switch.Scripts.Currency
{
    public class CurrencyMapper
    {
        private static readonly Dictionary<CurrencyType, string> EnumToString = new()
        {
            [CurrencyType.gold] = "gold",
            [CurrencyType.crystal] = "crystal",
            [CurrencyType.diamond] = "diamond",
            [CurrencyType.energy] = "energy",

            [CurrencyType.arena_token] = "arena_token",
            [CurrencyType.guild_coin] = "guild_coin",
            [CurrencyType.event_token] = "event_token",

            [CurrencyType.summon_ticket] = "summon_ticket",
            [CurrencyType.advanced_summon_ticket] = "advanced_summon_ticket",

            [CurrencyType.hero_exp] = "hero_exp",
            [CurrencyType.user_exp] = "user_exp",

            [CurrencyType.hero_fragment] = "hero_fragment",
            [CurrencyType.epic_hero_fragment] = "epic_hero_fragment",
            [CurrencyType.legendary_hero_fragment] = "legendary_hero_fragment",

            [CurrencyType.equipment_upgrade_stone] = "equipment_upgrade_stone",
            [CurrencyType.gear_refinement_stone] = "gear_refinement_stone",
            [CurrencyType.breakthrough_stone] = "breakthrough_stone",
            [CurrencyType.transcendence_stone] = "transcendence_stone",

            [CurrencyType.equipment_chest] = "equipment_chest",
            [CurrencyType.rare_equipment_chest] = "rare_equipment_chest",
            [CurrencyType.epic_equipment_chest] = "epic_equipment_chest",
            [CurrencyType.legendary_equipment_chest] = "legendary_equipment_chest",

            [CurrencyType.dungeon_key] = "dungeon_key",
            [CurrencyType.boss_challenge_ticket] = "boss_challenge_ticket",

            [CurrencyType.event_dice] = "event_dice",
            [CurrencyType.bingo_ticket] = "bingo_ticket",

            [CurrencyType.treasure_map] = "treasure_map",

            [CurrencyType.gold_chest] = "gold_chest",
            [CurrencyType.diamond_chest] = "diamond_chest",

            [CurrencyType.resource_pack] = "resource_pack",

            [CurrencyType.pet_egg] = "pet_egg",
            [CurrencyType.pet_upgrade_food] = "pet_upgrade_food",

            [CurrencyType.relic_fragment] = "relic_fragment",
            [CurrencyType.relic_upgrade_stone] = "relic_upgrade_stone",

            [CurrencyType.monster_card] = "monster_card",
            [CurrencyType.monster_essence] = "monster_essence",

            [CurrencyType.artifact_fragment] = "artifact_fragment",
            [CurrencyType.artifact_upgrade_core] = "artifact_upgrade_core",

            [CurrencyType.resource_coin] = "resource_coin",

            [CurrencyType.roulette_ticket] = "roulette_ticket",

            [CurrencyType.offline_reward_chest] = "offline_reward_chest",
            [CurrencyType.quick_loot_ticket] = "quick_loot_ticket",

            [CurrencyType.awakening_fruit] = "awakening_fruit",

            [CurrencyType.weapon_ore] = "weapon_ore",
            [CurrencyType.weapon_gem] = "weapon_gem",
            [CurrencyType.weapon_essence] = "weapon_essence",

            [CurrencyType.summon_ticket_hero] = "summon_ticket_hero",
            [CurrencyType.summon_ticket_weapon] = "summon_ticket_weapon",
        };

        private static readonly Dictionary<string, CurrencyType> StringToEnum =
            EnumToString.ToDictionary(
                pair => pair.Value,
                pair => pair.Key,
                StringComparer.OrdinalIgnoreCase);

        public static string Parse(CurrencyType type)
        {
            return EnumToString.TryGetValue(type, out var value)
                ? value
                : string.Empty;
        }

        public static bool TryParse(CurrencyType type, out string value)
        {
            return EnumToString.TryGetValue(type, out value);
        }

        public static CurrencyType Parse(string key)
        {
            return StringToEnum.TryGetValue(key, out var value)
                ? value
                : throw new ArgumentException($"Unknown currency key: {key}", nameof(key));
        }

        public static bool TryParse(string key, out CurrencyType value)
        {
            return StringToEnum.TryGetValue(key, out value);
        }
    }
}