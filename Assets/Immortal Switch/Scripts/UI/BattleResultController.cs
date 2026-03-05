using System;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts.UI
{
    public class BattleResultController : MonoBehaviour
    {
        [SerializeField] Button confirmBtn;

        private Action confirmAct = null;

        void Start()
        {
            confirmBtn?.onClick.AddListener(OnConfirmBtnClick);
            gameObject.SetActive(false);
        }

        public void RegisterConfirmAction(Action endAct)
        {
            confirmAct = endAct;
        }

        private void OnConfirmBtnClick()
        {
            confirmAct?.Invoke();
            SetBattleResultState(false);
            //confirmAct = null;
        }

        private void SetBattleResultState(bool isEnable)
        {
            gameObject.SetActive(isEnable);
        }

        public void ShowBattleResult(bool isWin = true)
        {
            SetBattleResultState(true);
        }

    }
}
