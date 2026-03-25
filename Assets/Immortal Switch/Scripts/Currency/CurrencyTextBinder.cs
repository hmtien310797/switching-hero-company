using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Currency
{
    public class CurrencyTextBinder : MonoBehaviour
    {
        [SerializeField] private CurrencyType currencyType;
        [SerializeField] private TMP_Text amountText;

        private void Awake()
        {
            if (amountText == null)
                amountText = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;

            Refresh();
        }

        private void OnDisable()
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
        }

        private void HandleCurrencyChanged(CurrencyChangedArgs args)
        {
            if (args.CurrencyType != currencyType) return;
            Refresh();
        }

        private void Refresh()
        {
            if (CurrencyManager.Instance == null || amountText == null) return;
            amountText.text = CurrencyManager.Instance.Get(currencyType).ToString();
        }
    }
}