using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Controller;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Popup;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.UI;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Layout
{
    public class EventLeHoiBangLongSummonLayout : MonoBehaviour
    {
        [SerializeField]
        private UICountdownTimer countdownTimer;

        [SerializeField]
        private Button btnExchangePoint;

        [Header("Summon references")]
        [SerializeField]
        private Toggle toggleSkipAnimation;

        [SerializeField]
        private UIEventLeHoiBangLongSummonButton btnX1;

        [SerializeField]
        private UIEventLeHoiBangLongSummonButton btnX10;

        [Header("Reward references")]
        [SerializeField]
        private TextMeshProUGUI txtDropRate;

        [SerializeField]
        private TextMeshProUGUI txtProgress;

        [SerializeField]
        private Image imgFill;

        // --- Private Fields ---
        private Action<EEventLeHoiBangLongLayoutType> _onChangeLayout;
        private int _maxPoint = 1;

        private void OnEnable()
        {
            EventLeHoiBangLongManager.Instance.OnDataChanged += RefreshProgress;
        }

        private void OnDisable()
        {
            EventLeHoiBangLongManager.Instance.OnDataChanged -= RefreshProgress;
        }

        private void Awake()
        {
            btnExchangePoint.onClick.AddListener(OnClickExchangePoint);
            btnX1.Bind(1, OnClickSummon);
            btnX10.Bind(10, OnClickSummon);
        }

        private async void OnClickSummon(int times)
        {
            var rewards = await TrySummon(times);

            if (rewards.Count > 0)
            {
                UIManager.Instance
                    .OpenPopupAsync<PopupEventLeHoiBangLongSummonView>(new PopupEventLeHoiBangLongSummonArgs(
                        toggleSkipAnimation.isOn,
                        TrySummon,
                        rewards
                    ))
                    .Forget();
            }
        }

        private UniTask<List<ItemData>> TrySummon(int times)
        {
            return EventLeHoiBangLongManager.Instance.SummonAsync(times);
        }

        private void OnDestroy()
        {
            btnExchangePoint.onClick.RemoveListener(OnClickExchangePoint);
        }

        private void OnClickExchangePoint()
        {
            _onChangeLayout?.Invoke(EEventLeHoiBangLongLayoutType.Mission);
        }

        public void Bind(
            Action<EEventLeHoiBangLongLayoutType> onChangeLayout,
            int maxPoint,
            double remainTime
        )
        {
            _onChangeLayout = onChangeLayout;
            _maxPoint = Math.Max(1, maxPoint);

            countdownTimer.Bind(remainTime, OnCountdown);
            RefreshProgress();
        }

        private string OnCountdown(long days, long hours, long minutes, long seconds)
        {
            return $"Kết thúc sau: {days:00} ngày {hours:00}:{minutes:00}:{seconds:00}";
        }

        private void RefreshProgress()
        {
            var accumulatedPoint = EventLeHoiBangLongManager.Instance.State?.Progress?.SummonPoints ?? 0;
            txtProgress.text = $"{accumulatedPoint:N0}/{_maxPoint:N0}";
            imgFill.fillAmount = accumulatedPoint / (_maxPoint * 1f);

            txtDropRate.text =
                "Tăng tỷ lệ nhận <color=#ff56ed><i><size=55>Băng Long</size></i></color>!\n" +
                $"Mỗi <color=#ffd200><i><size=55><b>{10 - (accumulatedPoint % 10)}</b></size></i></color> lượt chắc chắn nhận\n" +
                "<color=#ff56ed><i><size=55>Legend</size></i></color> trở lên";
        }
    }
}