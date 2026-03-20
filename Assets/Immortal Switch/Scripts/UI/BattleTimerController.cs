using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts.UI
{
    public class BattleTimerController : MonoBehaviour
    {
        [SerializeField] TMP_Text timerTxt;
        [SerializeField] Image timerSlide;

        private Action timeOutAct;
        private float oTime = 0;
        private float curTime = 0;
        private float lastTime = 0;
        private Tween timerTween;

        public void InitTimer(float dur, Action rAct)
        {
            oTime = dur;
            curTime = oTime;
            timeOutAct = rAct;

            timerTween?.Kill();

            UpDateTimerSlide(curTime);
            ShowTimer();

            timerTween = DOVirtual.Float(oTime, 0, oTime, value =>
                {
                    curTime = value;
                    UpDateTimerSlide(curTime);
                })
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    oTime = 0;
                    timeOutAct?.Invoke();
                });
        }

        private void UpDateTimerSlide(float dur)
        {
            if (oTime <= 0) return;

            var tTime = Mathf.RoundToInt(dur);

            if (tTime != lastTime)
            {
                lastTime = tTime;
            }

            timerSlide.fillAmount = dur / oTime;

            // hiển thị 1 số sau dấu phẩy
            timerTxt.text = dur.ToString("F1");
        }

        private void SetTimerState(bool isEnable)
        {
            gameObject.SetActive(isEnable);
        }

        private void ShowTimer()
        {
            SetTimerState(true);
        }

        public void HideTimer()
        {
            timerTween?.Kill();
            SetTimerState(false);
        }
    }
}
