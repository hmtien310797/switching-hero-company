using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shared.UI
{
    public class UIToggleGroup : MonoBehaviour
    {
        [SerializeField]
        private List<Toggle> toggles;

        // --- Private Fields ---
        private Action<bool, int> _onValueChanged;
        private UnityAction<bool>[] _listeners;

        private void Awake()
        {
            _listeners = new UnityAction<bool>[toggles.Count];

            for (int i = 0; i < toggles.Count; i++)
            {
                var index = i;

                _listeners[i] = isOn =>
                {
                    if (isOn)
                    {
                        OnToggleSelected(index);
                    }
                };

                toggles[i].onValueChanged.AddListener(_listeners[i]);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < toggles.Count; i++)
            {
                toggles[i].onValueChanged.RemoveListener(_listeners[i]);
            }
        }

        private void OnToggleSelected(int index)
        {
            Debug.Log($"Selected: {index}");
            _onValueChanged?.Invoke(toggles[index].isOn, index);
        }

        public void Bind(Action<bool, int> onValueChanged, int defaultIdx = 0)
        {
            _onValueChanged = onValueChanged;

            if (defaultIdx >= 0 &&
                defaultIdx < toggles.Count)
            {
                toggles[defaultIdx].SetIsOnWithoutNotify(true);
                OnToggleSelected(defaultIdx);
            }
        }
    }
}