using System;
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

        private void Start()
        {
            SetTimerState(false);
        }

        public void InitTimer(float dur, Action rAct)
        {
            oTime = dur;
            curTime = oTime;
            timeOutAct = rAct;
            UpDateTimerSlide(curTime);
            ShowTimer();
        }

        private void Update()
        {
            if (oTime == 0) return;

            curTime -= Time.deltaTime;
            UpDateTimerSlide(curTime);
            if(curTime <= 0)
            {
                timeOutAct?.Invoke();
                oTime = 0;
            }
        }

        private void UpDateTimerSlide(float dur)
        {
            if(oTime <= 0) return;
            
            var tTime = Mathf.RoundToInt(dur);
            if (tTime == lastTime) return;

            timerSlide.fillAmount = dur / oTime;
            lastTime = tTime;
            timerTxt.text = Mathf.RoundToInt(dur).ToString();
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
            SetTimerState(false);
        }
    }
}
