using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.UI
{
    public class UIEventLeHoiBangLongSummonButton : MonoBehaviour
    {
        [SerializeField]
        private Button btn;

        [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField]
        private TextMeshProUGUI txtCount;

        // --- Private Fields ---
        private Action<int> _onClickSummon;

        private int _times;

        private void Awake()
        {
            btn.onClick.AddListener(OnClickSummon);
        }

        private void OnDestroy()
        {
            btn.onClick.RemoveListener(OnClickSummon);
        }

        private void OnClickSummon()
        {
            _onClickSummon?.Invoke(_times);
        }

        public void Bind(int times, Action<int> onClickSummon)
        {
            _times = times;
            _onClickSummon = onClickSummon;

            txtTitle.text = $"Triệu Hồi X{times}";
            txtCount.text = $"{times}";
        }
    }
}