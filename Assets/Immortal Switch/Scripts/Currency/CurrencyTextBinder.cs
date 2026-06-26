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
            
            if (CurrencyLedgerService.Instance != null)
            {
                CurrencyLedgerService.Instance.OnCurrencyLedgerChanged += HandleCurrencyChanged;
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
                CurrencyLedgerService.Instance.OnCurrencyLedgerChanged -= HandleCurrencyChanged;
            }

            if (rewardSyncService != null)
            {
                rewardSyncService.OnOnlineIdlePreviewChanged -= Refresh;
            }
        }

        private void HandleCurrencyChanged(CurrencyLedgerChangedArgs args)
        {
            if (args.CurrencyType != currencyType)
                return;

            Refresh();
        }

        private void Refresh()
        {
            if (amountText == null)
                return;
            
            BigNumber displayAmount = CurrencyLedgerService.Instance != null ? CurrencyLedgerService.Instance.GetDisplayBalance(currencyType) : BigNumber.Zero;

            amountText.text = displayAmount.ToInputString();
        }

        private string FormatCurrency(BigNumber amount)
        {
            return amount.ToInputString();
        }
    }
}