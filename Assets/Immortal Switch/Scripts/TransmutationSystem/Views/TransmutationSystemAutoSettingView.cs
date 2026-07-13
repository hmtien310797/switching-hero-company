using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.TransmutationSystem.Views.UI;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views
{
    public class TransmutationSystemAutoSettingView : AnimatedUIView
    {
        [Header("View references")] [SerializeField]
        private Toggle toggleWaitingMaterial;

        [SerializeField] private Button btnStart;

        [Header("Presets")] [SerializeField] private List<UITransmutationCountTabPreset> tabPresets;

        [Header("Tier references")] [SerializeField]
        private Transform tabTierContainer;

        [SerializeField] private UITransmutationTierOption tabTierPrefab;

        [Header("Unique references")] [SerializeField]
        private UITransmutationUniqueOptionLayout uniqueOptionLayout1;

        [SerializeField] private UITransmutationUniqueOptionLayout uniqueOptionLayout2;

        // --- Private Fields ---
        private readonly List<UITransmutationTierOption> _tabTiers = new();
        private UITransmutationCountTabPreset _selectedCountTabPreset;
        private UITransmutationTierOption _selectedTabTiers;

        private void Awake()
        {
            toggleWaitingMaterial.onValueChanged.AddListener(ToggleWaitingMaterialChanged);
            btnStart.onClick.AddListener(OnClickStart);

            InitTabTiers();
            InitTabPreset();
            InitUniqueOptions();

            OnClickTabPreset(0);
            OnClickTabTiers(0);
        }

        private void OnClickStart()
        {
            TransmutationSystemManager.Instance.SaveSetting(
                new List<List<string>>
                {
                    uniqueOptionLayout1.SelectedUniqueOptions,
                    uniqueOptionLayout2.SelectedUniqueOptions,
                },
                _selectedCountTabPreset.Count,
                _selectedTabTiers.Tier,
                toggleWaitingMaterial.isOn,
                true
            );

            UIManager.Instance.TogglePopupAsync<TransmutationSystemAutoSettingView>().Forget();
        }

        private void ToggleWaitingMaterialChanged(bool newValue)
        {
            TransmutationSystemManager.Instance.SetWaitingMaterial(newValue);
        }

        private void InitTabPreset()
        {
            var counts = TransmutationSystemManager.Instance.GetCounts();

            for (var i = 0; i < counts.Count; i++)
            {
                var data = counts.ElementAtOrDefault(i);

                if (tabPresets.Count > i)
                {
                    var item = tabPresets[i];
                    item.Bind(i, data.Key.ToString(), OnClickTabPreset);
                    item.BindCount(data.Key);
                    item.SetStatus(data.Value);
                }
            }

            for (int i = counts.Count; i < tabPresets.Count; i++)
            {
                if (tabPresets.Count > i)
                {
                    tabPresets[i].SetStatus(ETabPresetStatus.Lock);
                }
            }
        }

        private void OnClickTabPreset(int idx)
        {
            if (_selectedCountTabPreset != null)
            {
                _selectedCountTabPreset.SetStatus(ETabPresetStatus.Normal);
                _selectedCountTabPreset = null;
            }

            _selectedCountTabPreset = tabPresets[idx];
            _selectedCountTabPreset.SetStatus(ETabPresetStatus.Selected);
        }

        private void OnClickTabTiers(int idx)
        {
            if (_selectedTabTiers != null)
            {
                _selectedTabTiers.SetStatus(ETabPresetStatus.Normal);
                _selectedTabTiers = null;
            }

            _selectedTabTiers = _tabTiers[idx];
            _selectedTabTiers.SetStatus(ETabPresetStatus.Selected);
        }

        private void InitTabTiers()
        {
            var entries = DatabaseManager.Instance.ItemTierDb.entries;

            for (var i = 0; i < entries.Length; i++)
            {
                var status = TransmutationSystemManager.Instance.IsUnlockGradeOption(entries[i].tier);

                if (_tabTiers.Count > i)
                {
                    var clone = _tabTiers[i];
                    clone.gameObject.SetActive(true);
                    clone.Bind(entries[i].tier, entries[i].tierIcon, i, string.Empty, OnClickTabTiers);
                    clone.SetStatus(status);
                }
                else
                {
                    var clone = Instantiate(tabTierPrefab, tabTierContainer);
                    clone.Bind(entries[i].tier, entries[i].tierIcon, i, string.Empty, OnClickTabTiers);
                    clone.SetStatus(status);
                    _tabTiers.Add(clone);
                }
            }
        }

        private void InitUniqueOptions()
        {
            var entries = new List<DynamicHeroesGlobalSpecificationsTransmuationUniqueRow>(
                DatabaseManager.Instance.TransmutationSystemDatabase.UniqueConfig.rows
            );

            entries.Insert(0, new DynamicHeroesGlobalSpecificationsTransmuationUniqueRow
            {
                statName = "Chưa chọn",
                uniqueId = string.Empty,
                displayOrder = 0,
                isActive = true,
                statGroup = string.Empty,
                valueType = string.Empty,
            });

            uniqueOptionLayout1.Bind(entries);
            uniqueOptionLayout2.Bind(entries);
        }
    }
}