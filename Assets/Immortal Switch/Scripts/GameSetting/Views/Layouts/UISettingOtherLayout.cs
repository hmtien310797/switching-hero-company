using System.Collections.Generic;
using Immortal_Switch.Scripts.Shared.UI;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.GameSetting.Views.Layouts
{
    public class UISettingOtherLayout : MonoBehaviour
    {
        [Header("Screen references")]
        [SerializeField]
        private UIToggleGroup tgOffscreen;

        [SerializeField]
        private List<TextMeshProUGUI> txtOffscreenTimes;

        [Header("Event noty references")]
        [SerializeField]
        private UIToggle toggleEventNoty;

        // --- Private Fields ---
        // time tính bằng phút, -1 là tắt
        private List<int> _offscreenTime = new()
        {
            // tắt
            -1,
            3,
            5,
            10,
            30,
        };

        private void Awake()
        {
            RefreshOffscreenTime();
            RefreshCurrent();
        }

        private void OnOffscreenValueChanged(bool arg1, int arg2)
        {
            if (arg1)
            {
                SettingManager.Instance.SetOffscreenTimeIdx(_offscreenTime[arg2]);
            }
        }

        private void RefreshOffscreenTime()
        {
            for (int i = 0; i < _offscreenTime.Count; i++)
            {
                var time = _offscreenTime[i];
                txtOffscreenTimes[i].text = time > 0 ? $"{time}M" : "Tắt";
            }
        }

        private void RefreshCurrent()
        {
            var current = SettingManager.Instance.CurrentSetting;
            toggleEventNoty.Bind(OnEventNotyToggleChanged, current.EventNotiEnabled);

            var timeIdx = _offscreenTime.FindIndex(v => v == current.OffscreenIdx);
            tgOffscreen.Bind(OnOffscreenValueChanged, timeIdx);
        }

        private void OnEventNotyToggleChanged(bool isOn)
        {
            SettingManager.Instance.SetEventNotiEnabled(isOn);
        }
    }
}