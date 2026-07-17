using System;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shared.Constants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventWheel.UI
{
    public class UIEventWheelButtonSpin : MonoBehaviour
    {
        [SerializeField]
        private Button btn;

        [SerializeField]
        private TextMeshProUGUI txtTimes;

        [SerializeField]
        private TextMeshProUGUI txtTicket;

        // --- Private Fields ---
        private Action<int> _onClickSpin;
        private int _times;

        private void Awake()
        {
            btn.onClick.AddListener(OnClickSpin);
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += OnLanguageChanged;
            RefreshLocalizedText();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(string langCode)
        {
            RefreshLocalizedText();
        }

        private void OnDestroy()
        {
            btn.onClick.RemoveListener(OnClickSpin);
        }

        private void OnClickSpin()
        {
            _onClickSpin?.Invoke(_times);
        }

        public void Bind(int times, string ticket, Action<int> onClickSpin)
        {
            _times = times;
            _onClickSpin = onClickSpin;

            txtTicket.text = ticket;
            RefreshLocalizedText();
        }

        private void RefreshLocalizedText()
        {
            txtTimes.text = LocalizationManager.GetText(LocalizationKeys.UI_WHEEL, _times);
        }
    }
}