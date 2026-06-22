using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Shared.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views.UI
{
    public class UITransmutationUniqueOptionLayout : MonoBehaviour
    {
        [Header("Unique references")] [SerializeField]
        private Transform uniqueOptionContainer;

        [SerializeField] private UITransmutationUniqueOptionEntry uniqueOptionPrefab;

        // so lan selection multiply
        [SerializeField] private int maxSelection = 2;

        // --- Private Fields ---
        private List<UITransmutationUniqueOptionEntry> _uniqueOptions = new();
        private List<UITransmutationUniqueOptionEntry> _selectedUniqueOptions = new();

        // lay ra ds unique da chon
        public List<string> SelectedUniqueOptions =>
            _selectedUniqueOptions
                .Where(v => !string.IsNullOrWhiteSpace(v.CurrentStat))
                .Select(v => v.CurrentStat)
                .ToList();

        public void Bind(List<DynamicHeroesGlobalSpecificationsTransmuationUniqueRow> rows)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                if (_uniqueOptions.Count > i)
                {
                    var clone = _uniqueOptions[i];
                    clone.gameObject.SetActive(true);
                    clone.Bind(i, rows[i].statName, OnClickUniqueOption);
                    clone.BindStat(rows[i].uniqueId);
                    clone.SetStatus(ETabPresetStatus.Normal);
                }
                else
                {
                    var clone = Instantiate(uniqueOptionPrefab, uniqueOptionContainer);
                    clone.Bind(i, rows[i].statName, OnClickUniqueOption);
                    clone.BindStat(rows[i].uniqueId);
                    clone.SetStatus(ETabPresetStatus.Normal);
                    _uniqueOptions.Add(clone);
                }
            }

            OnClickUniqueOption(0);
        }

        public bool IsMatch(string stat)
        {
            return _uniqueOptions.Any(v => v.IsMatch(stat));
        }

        private void OnClickUniqueOption(int idx)
        {
            // neu chon vao o rong, reset cac option da chon thanh normal
            if (idx == 0)
            {
                ResetSelectedOptions();
            }
            else
            {
                // remove trang thai cua cuc none va xoa ra khoi ds da chon
                for (int i = 0; i < _selectedUniqueOptions.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(_selectedUniqueOptions[i].CurrentStat))
                    {
                        _selectedUniqueOptions[i].SetStatus(ETabPresetStatus.Normal);
                        _selectedUniqueOptions.RemoveAt(i);
                        break;
                    }
                }
            }

            if (_uniqueOptions.Count > idx)
            {
                var item = _uniqueOptions[idx];

                if (_selectedUniqueOptions.Count >= maxSelection)
                {
                    _selectedUniqueOptions[^1].SetStatus(ETabPresetStatus.Normal);
                    _selectedUniqueOptions.RemoveAt(_selectedUniqueOptions.Count - 1);
                }

                item.SetStatus(ETabPresetStatus.Selected);
                _selectedUniqueOptions.Insert(0, item);
            }
            else
            {
                Debug.LogError("UniqueOption clicked index out of range");
            }
        }

        /// <summary>
        /// reset cac option khac thanh trang thai normal, ngoai tru none.
        /// </summary>
        private void ResetSelectedOptions()
        {
            var tmp = new List<UITransmutationUniqueOptionEntry>(_selectedUniqueOptions);

            foreach (var entry in tmp.Where(entry => !string.IsNullOrWhiteSpace(entry.CurrentStat)))
            {
                entry.SetStatus(ETabPresetStatus.Normal);
                _selectedUniqueOptions.RemoveAt(0);
            }
        }
    }
}