using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.StatSystem;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using Random = System.Random;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views.UI
{
    public class UITransmutationSystemReplaceInfoPanel : MonoBehaviour
    {
        [Header("View references")] [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField] private GameObject goUsedLayout;
        [SerializeField] private GameObject goEmptyLayout;

        [Header("Selected item references")] [SerializeField]
        private UITransmutationEquipment selectedEquip;

        [Header("Equip & Unique Effect")] [SerializeField]
        private Transform baseStatLineContainer;

        [SerializeField] private Transform uniqueStatLineContainer;
        [SerializeField] private UITransmutationStatLine statLinePrefab;

        // --- Private Field ---
        private List<UITransmutationStatLine> _statLines = new();

        public void Bind(PlayerEquipViewData showEquip, [CanBeNull] PlayerEquipViewData oldEquip, bool isUsed)
        {
            var showEquipCfg = DatabaseManager.Instance.EquipmentTierDatabase.Get(showEquip.ParsedTier);
            txtTitle.SetText(showEquip.Title);
            goUsedLayout.SetActive(isUsed);
            selectedEquip.Bind(showEquipCfg, showEquip.Level);

            var anyUnique = RebuildStats(showEquip, oldEquip, isUsed);
            goEmptyLayout.SetActive(!anyUnique);
        }

        private bool RebuildStats(PlayerEquipViewData equipment, [CanBeNull] PlayerEquipViewData oldEquip, bool hideUp)
        {
            var anyUnique = false;

            for (var idx = 0; idx < equipment.Modifiers.Count; idx++)
            {
                var modifier = equipment.Modifiers[idx];
                var isUp = hideUp ? null : IsUp(equipment, oldEquip, modifier.StatType, modifier.IsUnique);

                if (modifier.IsUnique &&
                    !anyUnique)
                {
                    anyUnique = true;
                }

                BuildStatLine(modifier, idx, modifier.IsUnique, isUp);
            }

            // an cac object con lại.
            for (var i = equipment.Modifiers.Count; i < _statLines.Count; i++)
            {
                var clone = _statLines[i];
                clone.gameObject.SetActive(false);
            }

            return anyUnique;
        }

        private bool? IsUp(PlayerEquipItem equipment, PlayerEquipItem oldEquip, StatType type, bool isUnique)
        {
            if (oldEquip == null)
            {
                return true;
            }

            var oldModifier = oldEquip.Modifiers.Find(v => v.StatType == type && v.IsUnique == isUnique);
            var newModifier = equipment.Modifiers.Find(v => v.StatType == type && v.IsUnique == isUnique);

            if (oldModifier != null &&
                newModifier != null)
            {
                return Mathf.Approximately(oldModifier.Value, newModifier.Value)
                    ? null
                    : oldModifier.Value < newModifier.Value;
            }

            return true;
        }

        private void BuildStatLine(StatModifier modifier, int idx, bool isUnique, bool? isUp)
        {
            var parent = isUnique ? uniqueStatLineContainer : baseStatLineContainer;
            var uniqueCfg = TransmutationSystemManager.Instance.GetUniqueCfg(modifier.StatType, modifier.Operation);

            if (_statLines.Count > idx)
            {
                var clone = _statLines[idx];
                clone.gameObject.SetActive(true);
                clone.transform.SetParent(parent);
                clone.Bind(isUnique, isUp, uniqueCfg?.statName ?? $"{idx}", Convert.ToInt64(modifier.Value));
            }
            else
            {
                var clone = Instantiate(statLinePrefab, parent);
                clone.Bind(isUnique, isUp, uniqueCfg?.statName ?? $"{idx}", Convert.ToInt64(modifier.Value));
                _statLines.Add(clone);
            }
        }
    }
}