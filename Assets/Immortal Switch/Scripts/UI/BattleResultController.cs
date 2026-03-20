using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts.UI
{
    public class BattleResultController : MonoBehaviour
    {
        [SerializeField] Button confirmBtn;

        private List<Action> confirmActs = new();

        private void Awake()
        {
            GameEventManager.Subscribe(GameEvents.OnStageCleared, ()=>SetBattleResultState(true));
        }


        void Start()
        {
            confirmBtn?.onClick.AddListener(OnConfirmBtnClick);
            gameObject.SetActive(false);
        }

        public void RegisterConfirmAction(Action endAct)
        {
            confirmActs.Add(endAct);
        }

        private void OnConfirmBtnClick()
        {
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
        }

        public void ShowBattleResult(bool isWin = true)
        {
            SetBattleResultState(true);
        }

    }
}
