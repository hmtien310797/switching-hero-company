using System.Collections.Generic;
using Immortal_Switch.Scripts.TransmutationSystem.Models;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views.UI
{
    public class UITransmutationTotalStatPanel : AnimatedUIView
    {
        [Header("Stat references")] [SerializeField]
        private Transform statLineContainer;

        [SerializeField] private UITransmutationTotalStatLine statLinePrefab;

        // --- Private Field ---
        private readonly List<UITransmutationTotalStatLine> _statLines = new();

        public void Bind(TransmutationSystemTotalStatData data)
        {
            for (var i = 0; i < data.Entries.Count; i++)
            {
                var item = data.Entries[i];

                if (_statLines.Count > i)
                {
                    var clone = _statLines[i];
                    clone.gameObject.SetActive(true);
                    clone.transform.SetParent(statLineContainer);
                    clone.Bind(item);
                }
                else
                {
                    var clone = Instantiate(statLinePrefab, statLineContainer);
                    clone.Bind(item);
                    _statLines.Add(clone);
                }
            }
        }
    }
}