using System;
using Immortal_Switch.Scripts.Localization;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.Views.UI
{
    public class UIEventItem : MonoBehaviour
    {
        [SerializeField]
        private Button btn;

        [SerializeField]
        private Image imgEvent;

        [SerializeField]
        private TextMeshProUGUI txtTitle;

        // --- Private Fields ---
        private Action<int> _onClick;
        private int _eventId;

        private void Awake()
        {
            btn.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            _onClick?.Invoke(_eventId);
        }

        public void Bind([CanBeNull] Sprite sprEvent, string titleKey, int eventId, Action<int> onClick)
        {
            _onClick = onClick;
            _eventId = eventId;

            imgEvent.sprite = sprEvent;
            txtTitle.text = LocalizationManager.GetText(titleKey);
        }
    }
}