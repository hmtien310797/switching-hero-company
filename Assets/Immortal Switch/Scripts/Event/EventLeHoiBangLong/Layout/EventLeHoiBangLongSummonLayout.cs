using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Controller;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Popup;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.UI;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared;
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

        private void OnClickSummon(int times)
        {
            UIManager.Instance
                .OpenPopupAsync<PopupEventLeHoiBangLongSummonView>(new PopupEventLeHoiBangLongSummonArgs(
                    times,
                    toggleSkipAnimation.isOn,
                    TrySummon
                ))
                .Forget();
        }

        private UniTask<List<ItemData>> TrySummon(int times)
        {
            var result = new List<ItemData>();

            for (int i = 0; i < times; i++)
            {
                var rate = DatabaseManager.Instance.EventBLRandomRate();

                if (rate != null)
                {
                    result.Add(new ItemData(rate.rewardId, rate.quantity));
                }
            }

            EventLeHoiBangLongManager.Instance.RecordEventSummon(times);
            return UniTask.FromResult(result);
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
            return $"Kết thúc sau: {days} ngày {hours}:{minutes}:{seconds}";
        }

        private void RefreshProgress()
        {
            var accumulatedPoint = EventLeHoiBangLongManager.Instance.Storage.Data.summonPoints;
            txtProgress.text = $"{accumulatedPoint:N0}/{_maxPoint:N0}";
            imgFill.fillAmount = accumulatedPoint / (_maxPoint * 1f);
        }
    }
}