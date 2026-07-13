using System;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shared.UI
{
    public class UIToggle : MonoBehaviour
    {
        [SerializeField]
        private Toggle toggle;

        [SerializeField]
        private GameObject onSelected;

        [SerializeField]
        private GameObject offSelected;

        // --- Private Fields ---
        private Action<bool> _onChanged;

        private void Awake()
        {
            toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        private void OnDestroy()
        {
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
        }

        public void Bind(Action<bool> onChanged, bool defaultValue = false)
        {
            _onChanged = onChanged;
            SetDefaultEnabled(defaultValue);
        }

        private void OnToggleChanged(bool isOn)
        {
            SetEnabled(isOn);
        }

        private void SetEnabled(bool isOn)
        {
            onSelected.SetActive(isOn);
            offSelected.SetActive(!isOn);
            _onChanged?.Invoke(isOn);
        }

        private void SetDefaultEnabled(bool isOn)
        {
            onSelected.SetActive(isOn);
            offSelected.SetActive(!isOn);
            toggle.SetIsOnWithoutNotify(isOn);
        }
    }
}