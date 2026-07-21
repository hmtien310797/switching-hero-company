using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Helper;
using Immortal_Switch.Scripts.Items.ScriptableObjects;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using Immortal_Switch.Scripts.Shared.Helper;
using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.TransmutationSystem.Interfaces;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Immortal_Switch.Scripts.TransmutationSystem
{
    internal class TransmutationSystemService : ITransmutationSystemService
    {
        private readonly ITransmutationSystemStorage _storage;

        public TransmutationSystemService(ITransmutationSystemStorage storage)
        {
            _storage = storage;
        }

        public void ApplyListResponse(TransmutationListResponse response)
        {
            _storage.Data.Level = response.Level;
            _storage.Data.Exp = response.Exp;
            _storage.Data.Energy = response.Energy;

            _storage.Data.Equips.Clear();
            if (response.Equips != null)
            {
                foreach (var dto in response.Equips)
                {
                    _storage.Data.Equips[dto.ItemType] = TransmutationSystemHelper.ToPlayerEquipItem(dto);
                }
            }

            _storage.Data.StuckEquip = TransmutationSystemHelper.ToPlayerEquipItem(response.Pending);

            if (response.AutoSetting != null &&
                Enum.TryParse<EItemTier>(response.AutoSetting.Tier, out var tier))
            {
                _storage.Data.Setting.Enabled       = response.AutoSetting.Enabled;
                _storage.Data.Setting.Count         = response.AutoSetting.Count;
                _storage.Data.Setting.Tier          = tier;
                _storage.Data.Setting.IsWaiting     = response.AutoSetting.IsWaiting;
                _storage.Data.Setting.UniqueOptions = response.AutoSetting.UniqueOptions ?? new List<List<string>>();
            }

            _storage.Save();
        }

        public void ApplyFuseResult(TransmutationFuseResponse response)
        {
            _storage.Data.Energy = response.EnergyBalance;
            _storage.Data.Exp = response.ExpBalance;
            _storage.Data.Level = response.Level;
            _storage.Data.StuckEquip = TransmutationSystemHelper.ToPlayerEquipItem(response.Pending);

            _storage.Save();
        }

        public void ApplyEquipResult(TransmutationEquipResponse response)
        {
            _storage.Data.Equips[response.ItemType] =
                TransmutationSystemHelper.ToPlayerEquipItem(response.ItemType, response.Equipped);
            _storage.Data.StuckEquip = null;

            _storage.Save();
        }

        public void ApplyDismantleResult(TransmutationDismantleResponse response)
        {
            _storage.Data.Energy = response.EnergyBalance;
            _storage.Data.Exp = response.ExpBalance;
            _storage.Data.Level = response.Level;
            _storage.Data.StuckEquip = null;

            _storage.Save();
        }

        public void Dismantle()
        {
            if (_storage.Data.StuckEquip == null)
            {
                return;
            }

            _storage.Data.StuckEquip = null;
            _storage.Save();
        }

        public EItemTier RollTier(DynamicHeroesGlobalSpecificationsTransmutationRateConfigRow row)
        {
            var weights = new Dictionary<EItemTier, float>
            {
                { EItemTier.D, row.d },
                { EItemTier.C, row.c },
                { EItemTier.B, row.b },
                { EItemTier.A, row.a },
                { EItemTier.S, row.s },
                { EItemTier.SS, row.sS },
                { EItemTier.SSS, row.sSS },
                { EItemTier.R, row.r },
                { EItemTier.SR, row.sR },
            };

            // loại bỏ các weight <= 0
            var validWeights = weights
                .Where(v => v.Value > 0)
                .ToList();

            if (validWeights.Count <= 0)
            {
                return EItemTier.D;
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

        public PlayerEquipItem BuildEquip(
            DynamicHeroesGlobalSpecificationsTransmuationItemConfigRow itemCfg,
            DynamicHeroesGlobalSpecificationsTransmutationRandomLevelRangeConfigRow levelRangeCfg,
            List<StatModifier> uniqueModifiers
        )
        {
            var rndLevel = Random.Range(levelRangeCfg.randomRangeMin, levelRangeCfg.randomRangeMax + 1);

            var level = Math.Clamp(
                levelRangeCfg.averageArtifactLevel + rndLevel,
                levelRangeCfg.finalLevelMin,
                levelRangeCfg.finalLevelMax
            );

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
            equip.Modifiers.AddRange(uniqueModifiers);

            _storage.Data.StuckEquip = equip;
            _storage.Save();
            return equip;
        }

        public PlayerEquipItem GetEquip(string itemType)
        {
            return _storage.Data.Equips.GetValueOrDefault(itemType);
        }

        public void ChangeStuck(PlayerEquipItem newStuck)
        {
            _storage.Data.StuckEquip = newStuck;
            _storage.Save();
        }

        public IEnumerable<PlayerEquipItem> GetEquips()
        {
            return _storage.Data.Equips.Values;
        }

        public void SetWaitingMaterial(bool value)
        {
            _storage.Data.Setting.IsWaiting = value;
            _storage.Save();
        }

        public bool ToggleWaitingMaterial()
        {
            _storage.Data.Setting.IsWaiting = !_storage.Data.Setting.IsWaiting;
            _storage.Save();
            return _storage.Data.Setting.IsWaiting;
        }

        public void SaveSetting(List<List<string>> uniqueOptions, int count, EItemTier tier, bool isWaiting, bool enabled)
        {
            _storage.Data.Setting.Enabled = enabled;
            _storage.Data.Setting.Count = count;
            _storage.Data.Setting.Tier = tier;
            _storage.Data.Setting.IsWaiting = isWaiting;
            _storage.Data.Setting.UniqueOptions = new List<List<string>>(uniqueOptions);
            _storage.Save();
        }

        public List<StatModifier> BuildUniqueModifiers(
            IReadOnlyList<DynamicHeroesGlobalSpecificationsTransmutationItemUniqueRow> rows,
            int count
        )
        {
            var result = new List<StatModifier>();

            if (rows.Count < 1)
            {
                return result;
            }

            for (var i = 0; i < count; i++)
            {
                var cfg = RandomHelper.RandomByWeight(rows, v => v.dropWeight);

                if (cfg == null)
                {
                    Debug.LogError($"BuildUniqueModifiers failed at index {i}");
                    continue;
                }

                var rndPct = Random.Range(cfg.rollMinPct, cfg.rollMaxPct);
                var mapping = TransmutationSystemHelper.ToStatMapping(cfg.statId);

                if (mapping != null)
                {
                    result.Add(new StatModifier(mapping.StatType, mapping.Op, rndPct, string.Empty, true));
                }
                else
                {
                    Debug.LogError($"BuildUniqueModifiers failed at index {i} with stat {cfg.statId}");
                }
            }

            return result;
        }
    }
}
