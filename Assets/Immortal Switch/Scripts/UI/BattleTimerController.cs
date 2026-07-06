using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
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

        private CancellationTokenRegistration timerCancellationRegistration;

        public void InitTimer(
            float duration,
            Action timeoutAction,
            CancellationToken cancellationToken)
        {
            StopTimer();

            oTime = Mathf.Max(0f, duration);
            curTime = oTime;
            timeOutAct = timeoutAction;

            UpDateTimerSlide(curTime);
            ShowTimer();

            if (cancellationToken.IsCancellationRequested)
            {
                timeOutAct = null;
                return;
            }

            if (oTime <= 0f)
            {
                CompleteTimer();
                return;
            }

            timerTween = DOVirtual.Float(
                    oTime,
                    0f,
                    oTime,
                    value =>
                    {
                        curTime = value;
                        UpDateTimerSlide(curTime);
                    })
                .SetEase(Ease.Linear)
                .OnComplete(CompleteTimer)
                .OnKill(() =>
                {
                    timerCancellationRegistration.Dispose();
                    timerTween = null;
                });

            if (cancellationToken.CanBeCanceled)
            {
                timerCancellationRegistration =
                    cancellationToken.Register(HideTimer);
            }
        }

        private void CompleteTimer()
        {
            timerCancellationRegistration.Dispose();

            timerTween = null;
            oTime = 0f;
            curTime = 0f;

            UpDateTimerSlide(curTime);

            Action callback = timeOutAct;
            timeOutAct = null;

            callback?.Invoke();
        }

        private void CancelTimer()
        {
            timeOutAct = null;

            if (timerTween != null &&
                timerTween.IsActive())
            {
                timerTween.Kill();
            }
        }

        private void StopTimer()
        {
            timerCancellationRegistration.Dispose();

            timeOutAct = null;

            if (timerTween != null &&
                timerTween.IsActive())
            {
                timerTween.Kill();
            }

            timerTween = null;
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
            try
            {
                StopTimer();
                SetTimerState(false);
            }
            catch (Exception e)
            {
                //stop timer by CTS
            }
        }
    }
}
