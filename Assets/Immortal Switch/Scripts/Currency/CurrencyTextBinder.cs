using System;
using Battle;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Reward;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Currency
{
    public class CurrencyTextBinder : MonoBehaviour
    {
        [SerializeField] private CurrencyType currencyType;
        [SerializeField] private TMP_Text amountText;

        [Header("Optional")] 
        [SerializeField] private bool includeOnlineIdlePreview = true;

        private RewardSyncService rewardSyncService;

        private void Start()
        {
            rewardSyncService = PvEBattleController.Instance.RewardSyncService;
            
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
                CurrencyManager.Instance.OnAnyCurrencyChanged += Refresh;
            }

            if (rewardSyncService != null)
            {
                rewardSyncService.OnOnlineIdlePreviewChanged += Refresh;
            }

            Refresh();
        }
        

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
                CurrencyManager.Instance.OnAnyCurrencyChanged -= Refresh;
            }

            if (rewardSyncService != null)
            {
                rewardSyncService.OnOnlineIdlePreviewChanged -= Refresh;
            }
        }

        private void HandleCurrencyChanged(CurrencyChangedArgs args)
        {
            if (args.CurrencyType != currencyType)
                return;

            Refresh();
        }

        public void Refresh()
        {
            if (amountText == null)
                return;

            BigNumber displayAmount;

            if (CurrencyLedgerService.Instance != null)
            {
                displayAmount = CurrencyLedgerService.Instance.GetDisplayBalance(currencyType);
            }
            else if (CurrencyManager.Instance != null)
            {
                displayAmount = CurrencyManager.Instance.Get(currencyType);
            }
            else
            {
                displayAmount = BigNumber.Zero;
            }

            amountText.text = displayAmount.ToInputString();
        }

        private string FormatCurrency(BigNumber amount)
        {
            return amount.ToInputString();
        }
    }
}