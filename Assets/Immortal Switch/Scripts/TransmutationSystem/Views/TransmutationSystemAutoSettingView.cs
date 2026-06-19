using System.Collections.Generic;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.TransmutationSystem.Views.UI;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views
{
    public class TransmutationSystemAutoSettingView : AnimatedUIView
    {
        [Header("Presets")] [SerializeField] private List<UITabPreset> tabPresets;

        [Header("Tier references")] [SerializeField]
        private Transform tabTierContainer;

        [SerializeField] private UITransmutationTierSettingEntry tabTierPrefab;

        // --- Private Fields ---
        private List<UITransmutationTierSettingEntry> _tabTiers = new();
        private UITabPreset _selectedTabPreset;

        private void Awake()
        {
            InitTabTiers();
            InitTabPreset();
        }

        private void InitTabPreset()
        {
            for (var i = 0; i < tabPresets.Count; i++)
            {
                tabPresets[i].Bind(i, $"{i + 1}", OnClickTabPreset);
            }
        }

        private void OnClickTabPreset(int idx)
        {
            if (_selectedTabPreset != null)
            {
                _selectedTabPreset.SetStatus(ETabPresetStatus.Normal);
                _selectedTabPreset = null;
            }

            _selectedTabPreset = tabPresets[idx];
            _selectedTabPreset.SetStatus(ETabPresetStatus.Selected);
        }

        private void InitTabTiers()
        {
            var entries = DatabaseManager.Instance.EquipmentTierDatabase.entries;

            for (var i = 0; i < entries.Length; i++)
            {
                if (_tabTiers.Count > i)
                {
                    var clone = _tabTiers[i];
                    clone.gameObject.SetActive(true);
                    clone.Bind(entries[i].tier, i, string.Empty, _ => { });
                }
                else
                {
                    var clone = Instantiate(tabTierPrefab, tabTierContainer);
                    clone.Bind(entries[i].tier, i, string.Empty, _ => { });
                    _tabTiers.Add(clone);
                }
            }
        }
    }
}