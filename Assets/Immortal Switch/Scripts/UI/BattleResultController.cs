using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    public class BattleResultController : MonoBehaviour
    {
        [SerializeField] Button confirmBtn;
        [SerializeField] Button autoNextBtn;
        [SerializeField] GameObject innerSelected;

        private List<Action> confirmActs = new();
        private bool isAutoActived = false;

        private void Awake()
        {
            GameEventManager.Subscribe(GameEvents.OnStageCleared, ()=>SetBattleResultState(true));
        }


        void Start()
        {
            confirmBtn?.onClick.AddListener(OnConfirmBtnClick);
            gameObject.SetActive(false);
            autoNextBtn?.onClick.AddListener(AutoNextCallback);
        }

        public void RegisterConfirmAction(Action endAct)
        {
            confirmActs.Add(endAct);
        }

        private void OnConfirmBtnClick()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            foreach (var action in confirmActs)
            {
                action?.Invoke();
            }

            confirmActs.Clear();
            SetBattleResultState(false);
            //confirmAct = null;
        }

        private void SetBattleResultState(bool isEnable)
        {
            gameObject.SetActive(isEnable);

            if(isEnable && isAutoActived)
            {
                Invoke(nameof(OnConfirmBtnClick), 3f);
            }
        }

        public void ShowBattleResult(bool isWin = true)
        {
            SetBattleResultState(true);
        }

        private void AutoNextCallback()
        {
            isAutoActived = innerSelected?.activeInHierarchy ?? false;
            if (innerSelected)
            {
                isAutoActived = !isAutoActived;
                innerSelected.SetActive(isAutoActived);
            }
        }
    }
}
