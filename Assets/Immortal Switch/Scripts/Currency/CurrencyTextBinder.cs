using Battle;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Items;
using Immortal_Switch.Scripts.Reward;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Currency
{
    public class CurrencyTextBinder : MonoBehaviour
    {
        [SerializeField]
        private CurrencyType currencyType;

        [SerializeField]
        private ECurrencyType eCurrencyType;

        [SerializeField]
        private TMP_Text amountText;

        [SerializeField]
        private Image currencyImage;

        [SerializeField]
        private Button btnInfo;

        // --- Private Fields ---
        private RewardSyncService rewardSyncService;

        private void Start()
        {
            rewardSyncService = PvEBattleController.Instance.RewardSyncService;

            if (CurrencyLedgerService.Instance != null)
            {
                CurrencyLedgerService.Instance.OnCurrencyLedgerChanged += HandleCurrencyChanged;
            }

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
            }

            if (rewardSyncService != null)
            {
                rewardSyncService.OnOnlineIdlePreviewChanged += Refresh;
            }

            if (btnInfo != null)
            {
                btnInfo.onClick.AddListener(OnClickItemInfo);
            }

            Refresh();
        }

        private void OnClickItemInfo()
        {
            var itemId = (int)eCurrencyType;

            if (itemId > 0)
            {
                UIManager.Instance
                    .OpenPopupAsync<PopupItemInfoView>(new PopupItemInfoArgs
                    {
                        ItemId = itemId,
                    })
                    .Forget();
            }
        }

        private void OnEnable()
        {
            if (currencyImage != null)
            {
                currencyImage.sprite = DatabaseManager.Instance.ItemDb.LoadIconByItemKey(currencyType.ToString());
            }
        }

        private void OnDestroy()
        {
            if (CurrencyLedgerService.Instance != null)
            {
                CurrencyLedgerService.Instance.OnCurrencyLedgerChanged -= HandleCurrencyChanged;
            }

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
            }

            if (rewardSyncService != null)
            {
                rewardSyncService.OnOnlineIdlePreviewChanged -= Refresh;
            }

            if (btnInfo != null)
            {
                btnInfo.onClick.RemoveListener(OnClickItemInfo);
            }
        }

        private void HandleCurrencyChanged(CurrencyChangedArgs args)
        {
            if (args.CurrencyType != currencyType)
                return;

            Refresh();
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

            BigNumber displayAmount = CurrencyLedgerService.Instance != null
                ? CurrencyLedgerService.Instance.GetDisplayBalance(currencyType)
                : BigNumber.Zero;

            if (displayAmount == 0)
            {
                displayAmount = ItemsManager.Instance.GetQuantity(eCurrencyType);
            }

            amountText.text = displayAmount.ToInputString();
        }
    }
}