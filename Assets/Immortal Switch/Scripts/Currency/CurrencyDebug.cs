using UnityEngine;

namespace Immortal_Switch.Scripts.Currency
{
    public class CurrencyDebug : MonoBehaviour
    {
        [SerializeField] private CurrencyType currencyType = CurrencyType.Gold;
        [SerializeField] private int amount = 100;

        [ContextMenu("Add Currency")]
        public void AddCurrency()
        {
            CurrencyManager.Instance.Add(currencyType, amount);
            Debug.Log($"Added {amount} {currencyType}. Current = {CurrencyManager.Instance.Get(currencyType)}");
        }

        [ContextMenu("Spend Currency")]
        public void SpendCurrency()
        {
            bool result = CurrencyManager.Instance.Spend(currencyType, amount);
            Debug.Log($"Spend {amount} {currencyType} => {result}. Current = {CurrencyManager.Instance.Get(currencyType)}");
        }

        [ContextMenu("Set Currency")]
        public void SetCurrency()
        {
            CurrencyManager.Instance.Set(currencyType, amount);
            Debug.Log($"Set {currencyType} = {CurrencyManager.Instance.Get(currencyType)}");
        }
    }
}