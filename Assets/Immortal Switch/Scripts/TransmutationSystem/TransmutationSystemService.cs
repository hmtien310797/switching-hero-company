using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.TransmutationSystem.Interfaces;
using UnityEngine;

namespace Immortal_Switch.Scripts.TransmutationSystem
{
    internal class TransmutationSystemService : ITransmutationSystemService
    {
        private readonly ITransmutationSystemStorage _storage;

        public TransmutationSystemService(ITransmutationSystemStorage storage)
        {
            _storage = storage;
        }

        public void AddExp(BigInteger quantity)
        {
            _storage.Data.UpdateExp(_storage.Data.Exp + quantity);
            _storage.Save();
        }

        public void AddEnergy(BigInteger quantity)
        {
            _storage.Data.UpdateEnergy(_storage.Data.Energy + quantity);
            _storage.Save();
        }

        public void LevelUp(int totalLevel)
        {
            var nextLevel = Mathf.Min(_storage.Data.Level + 1, totalLevel);
            _storage.Data.UpdateLevel(nextLevel);
            _storage.Save();
        }

        public string RollTier(DynamicHeroesGlobalSpecificationsTransmutationRateConfigRow row)
        {
            var weights = new Dictionary<string, float>
            {
                { TransmutationSystemTierConstants.D, row.d },
                { TransmutationSystemTierConstants.C, row.c },
                { TransmutationSystemTierConstants.B, row.b },
                { TransmutationSystemTierConstants.A, row.a },
                { TransmutationSystemTierConstants.S, row.s },
                { TransmutationSystemTierConstants.SS, row.sS },
                { TransmutationSystemTierConstants.SSS, row.sSS },
                { TransmutationSystemTierConstants.R, row.r },
                { TransmutationSystemTierConstants.SR, row.sR },
            };

            // loại bỏ các weight <= 0
            var validWeights = weights
                .Where(v => v.Value > 0)
                .ToList();

            if (validWeights.Count <= 0)
            {
                return TransmutationSystemTierConstants.D;
            }

            // tổng weight
            var totalWeight = validWeights.Sum(v => v.Value);

            // random từ 0 -> totalWeight
            var randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0;

            foreach (var weight in validWeights)
            {
                currentWeight += weight.Value;

                if (randomValue <= currentWeight)
                {
                    return weight.Key;
                }
            }

            // fallback
            return validWeights.Last().Key;
        }

        public PlayerEquipItem BuildEquip(DynamicHeroesGlobalSpecificationsTransmuationItemConfigRow itemCfg,
            DynamicHeroesGlobalSpecificationsTransmutationRandomLevelRangeConfigRow levelRangeCfg)
        {
            var rndLevel = Random.Range(levelRangeCfg.randomRangeMin, levelRangeCfg.randomRangeMax + 1);
            var level = levelRangeCfg.averageArtifactLevel + rndLevel;

            var equip = new PlayerEquipItem
            {
                CfgId = itemCfg.configId,
                ItemType = itemCfg.itemType,
                Tier = itemCfg.tier,
                Modifiers = new List<StatModifier>(),
                Level = level,
            };

            var stat1Rate = Random.Range(itemCfg.baseStat1Valuemin, itemCfg.baseStat1Valuemax);
            var stat2Rate = Random.Range(itemCfg.baseStat2Valuemin, itemCfg.baseStat2Valuemax);
            var stat3Rate = Random.Range(itemCfg.baseStat3Valuemin, itemCfg.baseStat3Valuemax);

            var mapping1 = TransmutationSystemHelper.ToStatMapping(itemCfg.baseStat1Type);
            var mapping2 = TransmutationSystemHelper.ToStatMapping(itemCfg.baseStat2Type);
            var mapping3 = TransmutationSystemHelper.ToStatMapping(itemCfg.baseStat3Type);

            equip.Modifiers.Add(new StatModifier(mapping1.StatType, mapping1.Op, stat1Rate));
            equip.Modifiers.Add(new StatModifier(mapping2.StatType, mapping2.Op, stat2Rate));
            equip.Modifiers.Add(new StatModifier(mapping3.StatType, mapping3.Op, stat3Rate));

            var uniqueOptions = new List<string>
            {
                itemCfg.uniqueOptionPool1,
                itemCfg.uniqueOptionPool2,
                itemCfg.uniqueOptionPool3,
            };

            foreach (var entry in uniqueOptions)
            {
                var rate = Random.Range(itemCfg.uniqueOptionCountMin, itemCfg.uniqueOptionCountMax);
                var mapping = TransmutationSystemHelper.ToStatMapping(entry);
                equip.Modifiers.Add(new StatModifier(mapping.StatType, mapping.Op, rate));
            }

            return equip;
        }

        public PlayerEquipItem GetEquip(string itemType)
        {
            return _storage.Data.Equips.GetValueOrDefault(itemType);
        }

        public IEnumerable<PlayerEquipItem> GetEquips()
        {
            return _storage.Data.Equips.Values;
        }

        public void Equip(PlayerEquipItem newEquip)
        {
            if (!_storage.Data.Equips.TryAdd(newEquip.ItemType, newEquip))
            {
                _storage.Data.Equips[newEquip.ItemType] = newEquip;
            }

            _storage.Save();
        }
    }
}