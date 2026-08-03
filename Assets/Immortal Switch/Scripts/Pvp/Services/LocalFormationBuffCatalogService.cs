using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Pvp.Data;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Services
{
    /// <summary>
    /// Phase-1 catalog service. Load <see cref="FormationBuffCatalogSO"/> từ
    /// <c>Resources/PvP/FormationBuffCatalog</c>; nếu thiếu, build default in-memory catalog từ
    /// <see cref="PvpTestBuffIds"/> để Phase-1 vẫn testable. Buff lookup theo BuffId.
    /// (DOCX §22. FLAGGED: production phải có SO asset.)
    /// </summary>
    internal sealed class LocalFormationBuffCatalogService : IFormationBuffCatalogService
    {
        private const string ResourcesPath = "PvP/FormationBuffCatalog";

        private readonly Dictionary<string, FormationBuffDataSO> _byId = new();
        private readonly List<FormationBuffDataSO> _list = new();
        private bool _initialized;

        public LocalFormationBuffCatalogService()
        {
            EnsureInitialized();
        }

        public IReadOnlyList<FormationBuffDataSO> GetAll()
        {
            EnsureInitialized();
            return _list;
        }

        public FormationBuffDataSO GetRequired(string buffId)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(buffId) || !_byId.TryGetValue(buffId, out var data))
            {
                Debug.LogError($"[PvP] FormationBuff '{buffId}' not found in catalog.");
                return null;
            }
            return data;
        }

        public bool TryGet(string buffId, out FormationBuffDataSO data)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(buffId))
            {
                data = null;
                return false;
            }
            return _byId.TryGetValue(buffId, out data);
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            var so = Resources.Load<FormationBuffCatalogSO>(ResourcesPath);
            if (so != null && so.Buffs != null && so.Buffs.Length > 0)
            {
                foreach (var b in so.Buffs)
                {
                    if (b == null || string.IsNullOrEmpty(b.BuffId)) continue;
                    if (_byId.ContainsKey(b.BuffId))
                    {
                        Debug.LogWarning($"[PvP] Duplicate BuffId '{b.BuffId}' in catalog — ignoring later entry.");
                        continue;
                    }
                    _byId[b.BuffId] = b;
                    _list.Add(b);
                }
                Debug.Log($"[PvP] FormationBuff catalog loaded from SO: {_list.Count} buffs.");
                return;
            }

            // FLAGGED: SO chưa tạo — fallback default catalog để Phase-1 testable.
            Debug.LogWarning($"[PvP] FormationBuffCatalogSO not found at Resources/{ResourcesPath}. Using built-in default test catalog — create the SO for production.");
            BuildDefaultCatalog();
        }

        private void BuildDefaultCatalog()
        {
            AddDefault(PvpTestBuffIds.IronCore, "Iron Core", BuffRarity.Common,
                BuffSlotType.Core, FormationBuffSlotCompat.Any, null,
                new FormationBuffStatEffect
                {
                    StatType = StatType.MaxHp, Operation = ModifierOp.Multiply,
                    BaseValue = 0.15f, ValuePerLevel = 0.03f
                });

            AddDefault(PvpTestBuffIds.ArcaneFlow, "Arcane Flow", BuffRarity.Rare,
                BuffSlotType.Support, FormationBuffSlotCompat.Any, null,
                new FormationBuffStatEffect
                {
                    StatType = StatType.CooldownReduction, Operation = ModifierOp.Add,
                    BaseValue = 0.04f, ValuePerLevel = 0.02f
                });

            // M6: Last Stand = Trigger (HpBelowThreshold 30%) → Shield (DOCX §18). Demonstrates buff runtime.
            AddTriggeredDefault(PvpTestBuffIds.LastStand, "Last Stand", BuffRarity.Epic,
                BuffSlotType.Trigger, FormationBuffSlotCompat.Any,
                FormationBuffTriggerType.HpBelowThreshold, 30f,
                new FormationBuffEffectData
                {
                    EffectType = FormationBuffEffectType.Shield,
                    Target = FormationBuffTargetType.Owner,
                    AmountBase = 500f, AmountPerLevel = 150f
                });

            AddDefault(PvpTestBuffIds.GuardianLink, "Guardian Link", BuffRarity.Uncommon,
                BuffSlotType.Support, FormationBuffSlotCompat.FrontOnly, null);
        }

        private void AddDefault(string buffId, string name, BuffRarity rarity,
            BuffSlotType slotType, FormationBuffSlotCompat compat, string groupId,
            params FormationBuffStatEffect[] effects)
        {
            var so = ScriptableObject.CreateInstance<FormationBuffDataSO>();
            so.BuffId = buffId;
            so.BuffNameKey = name;
            so.Rarity = rarity;
            so.SlotType = slotType;
            so.FormationSlotCompat = compat;
            so.GroupId = groupId;
            so.MaxLevel = 5;
            so.StatEffects = effects ?? Array.Empty<FormationBuffStatEffect>();
            _byId[buffId] = so;
            _list.Add(so);
        }

        /// <summary>Add a buff với trigger + triggered effects (M6 — DOCX §18).</summary>
        private void AddTriggeredDefault(string buffId, string name, BuffRarity rarity,
            BuffSlotType slotType, FormationBuffSlotCompat compat,
            FormationBuffTriggerType triggerType, float hpThreshold,
            params FormationBuffEffectData[] effects)
        {
            var so = ScriptableObject.CreateInstance<FormationBuffDataSO>();
            so.BuffId = buffId;
            so.BuffNameKey = name;
            so.Rarity = rarity;
            so.SlotType = slotType;
            so.FormationSlotCompat = compat;
            so.MaxLevel = 5;
            so.Trigger = new FormationBuffTriggerData
            {
                TriggerType = triggerType,
                HpThresholdPercent = hpThreshold,
                InternalCooldown = 6f
            };
            so.Effects = effects ?? Array.Empty<FormationBuffEffectData>();
            _byId[buffId] = so;
            _list.Add(so);
        }
    }
}
