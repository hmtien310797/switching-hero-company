using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Core;

namespace Immortal_Switch.Scripts.Currency
{
    public enum CurrencyType
    {
        none = 0,

        gold,
        crystal,
        diamond,
        energy,

        arena_token,
        guild_coin,
        event_token,

        summon_ticket,
        advanced_summon_ticket,

        hero_exp,

        hero_fragment,
        epic_hero_fragment,
        legendary_hero_fragment,

        equipment_upgrade_stone,
        gear_refinement_stone,
        breakthrough_stone,
        transcendence_stone,

        equipment_chest,
        rare_equipment_chest,
        epic_equipment_chest,
        legendary_equipment_chest,

        dungeon_key,
        boss_challenge_ticket,

        event_dice,
        bingo_ticket,

        treasure_map,

        gold_chest,
        diamond_chest,

        resource_pack,

        pet_egg,
        pet_upgrade_food,

        relic_fragment,
        relic_upgrade_stone,

        monster_card,
        monster_essence,

        artifact_fragment,
        artifact_upgrade_core,

        resource_coin,

        roulette_ticket,

        offline_reward_chest,
        quick_loot_ticket,

        awakening_fruit,

        weapon_ore,
        weapon_gem,
        weapon_essence,

        // ===== Legacy =====
        HeroTicket = summon_ticket,
        WeaponTicket,
        SkillTicket,

        WeaponEnhancementStone = equipment_upgrade_stone,
        WeaponBreakThroughStone = breakthrough_stone,

        ArcherWeaponTranscendenceStone = transcendence_stone,
        MageWeaponTranscendenceStone = transcendence_stone,
        WarriorWeaponTranscendenceStone = transcendence_stone,
        AssassinWeaponTranscendenceStone = transcendence_stone,
    }    
    [Serializable]
    public class CurrencyEntry
    {
        public CurrencyType CurrencyType;
        public BigNumber Amount;
    }

    [Serializable]
    public class CurrencySaveData
    {
        public List<CurrencyEntry> Entries = new();
    }
    
    public class CurrencyChangedArgs
    {
        public CurrencyType CurrencyType;
        public BigNumber OldAmount;
        public BigNumber NewAmount;
        public BigNumber Delta;
    }
}