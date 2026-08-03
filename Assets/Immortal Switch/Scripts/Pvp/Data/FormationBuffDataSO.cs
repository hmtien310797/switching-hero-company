using System;
using Immortal_Switch.Scripts.Pvp.Models;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Data
{
    /// <summary>
    /// Định nghĩa 1 Formation Buff (DOCX §17, §18, §19, §28). Phase-1 (M2) chứa: identity,
    /// slot metadata (SlotType / FormationSlotCompat / GroupId), MaxLevel, stat effects.
    /// Trigger/Condition/Target/Duration/Stack/Cooldown/Limit config được thêm ở M6
    /// (data-driven runtime — DOCX §18). BuffId phải duy nhất trong catalog.
    /// </summary>
    [CreateAssetMenu(menuName = "PvP/Formation Buff")]
    public sealed class FormationBuffDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string BuffId;
        public string BuffNameKey;
        public string IconKey;
        public BuffRarity Rarity = BuffRarity.Common;
        public int BuffDataVersion = 1;

        [Header("Slot Metadata (DOCX §17)")]
        public BuffSlotType SlotType = BuffSlotType.Core;
        public FormationBuffSlotCompat FormationSlotCompat = FormationBuffSlotCompat.Any;

        [Tooltip("Buff cùng GroupId không được equip cùng lúc trong 1 loadout (group conflict, DOCX §17).")]
        public string GroupId;

        [Header("Progression (DOCX §21 — default max level 5)")]
        public int MaxLevel = 5;

        [Header("Stat Effects (passive — applied at battle start, M5+)")]
        public FormationBuffStatEffect[] StatEffects;

        [Header("Buff Runtime (M6 — DOCX §18: Trigger + Target + Effects + Duration/Cooldown/Stack)")]
        public FormationBuffTriggerData Trigger = new();
        public FormationBuffEffectData[] Effects;
    }
}
