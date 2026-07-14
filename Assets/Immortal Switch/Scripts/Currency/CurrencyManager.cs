using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Currency
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        [SerializeField] private List<CurrencyEntry> currencies = new List<CurrencyEntry>();

        public event Action<CurrencyChangedArgs> OnCurrencyChanged;
        public event Action OnAnyCurrencyChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitDefaultCurrencies();
        }

        private void InitDefaultCurrencies()
        {
            Array values = Enum.GetValues(typeof(CurrencyType));

            for (int i = 0; i < values.Length; i++)
            {
                CurrencyType type = (CurrencyType)values.GetValue(i);

                if (FindEntry(type) != null)
                    continue;

                currencies.Add(new CurrencyEntry
                {
                    CurrencyType = type,
                    Amount = BigNumber.Zero
                });
            }
        }

        public BigNumber Get(CurrencyType currencyType)
        {
            return GetEntry(currencyType).Amount;
        }

        public bool HasEnough(CurrencyType currencyType, BigNumber amount)
        {
            if (amount <= BigNumber.Zero)
                return true;

            return Get(currencyType) >= amount;
        }

        /// <summary>
        /// Gán trực tiếp số dư từ dữ liệu server (player data, summon result, ...).
        /// </summary>
        public void Set(CurrencyType currencyType, BigNumber amount)
        {
            CurrencyEntry entry = GetEntry(currencyType);
            BigNumber oldAmount = entry.Amount;

            if (oldAmount == amount)
                return;

            entry.Amount = amount;

            NotifyChanged(currencyType, oldAmount, amount);
        }

        /// <summary>
        /// Dùng cho debug/local demo. Server mode không nên gọi trực tiếp cho giao dịch thật.
        /// </summary>
        public void AddLocalDemo(CurrencyType currencyType, BigNumber amount)
        {
            if (amount <= BigNumber.Zero)
                return;

            CurrencyEntry entry = GetEntry(currencyType);
            BigNumber oldAmount = entry.Amount;
            BigNumber newAmount = oldAmount + amount;

            entry.Amount = newAmount;

            NotifyChanged(currencyType, oldAmount, newAmount);
        }

        /// <summary>
        /// Ghi đè balance tuyệt đối từ response server (battle/end, ...). gold/diamond/energy theo
        /// field riêng vì tên JSON không khớp tên CurrencyType ("diamonds" vs "diamond"); còn lại
        /// (items) parse theo tên CurrencyType — bỏ qua key không khớp enum nào (vd. item_id số).
        /// </summary>
        public void ApplyServerBalances(long gold, long diamonds, int crystal, IReadOnlyDictionary<string, double> items = null)
        {
            Set(CurrencyType.gold, BigNumber.FromDouble(gold));
            Set(CurrencyType.diamond, BigNumber.FromDouble(diamonds));
            Set(CurrencyType.crystal, BigNumber.FromDouble(crystal));
            
            UIManager.Instance.TogglePopupAsync<PopupRewardView>(new PopupRewardArgs
            {
                Rewards = new List<ItemRewardData>
                {
                    new(nameof(CurrencyType.gold), BigNumber.FromDouble(gold)),
                    new(nameof(CurrencyType.diamond), BigNumber.FromDouble(diamonds)),
                    new(nameof(CurrencyType.crystal), BigNumber.FromDouble(crystal)),
                }
            }, false).Forget();

            if (items == null)
                return;

            foreach (var kv in items)
            {
                if (TryParseCurrencyType(kv.Key, out CurrencyType type))
                    Set(type, BigNumber.FromDouble(kv.Value));
            }
        }

        /// <summary>
        /// Ghi đè balance tuyệt đối từ balances array của server (afk/claim).
        /// currency_type trong RewardDto khớp tên CurrencyType enum (case-insensitive).
        /// amount là string số nguyên.
        /// </summary>
        public void ApplyServerBalances(IReadOnlyList<RewardDto> balances)
        {
            if (balances == null) return;

            for (int i = 0; i < balances.Count; i++)
            {
                RewardDto b = balances[i];
                if (TryParseCurrencyType(b.CurrencyType, out CurrencyType type) &&
                    TryParseAmount(b.Amount, out BigNumber amount))
                    Set(type, amount);
            }
        }

        /// <summary>
        /// Dùng cho debug/local demo. Server mode không nên gọi trực tiếp cho giao dịch thật.
        /// </summary>
        public bool SpendLocalDemo(CurrencyType currencyType, BigNumber amount)
        {
            if (amount <= BigNumber.Zero)
                return true;

            CurrencyEntry entry = GetEntry(currencyType);

            if (entry.Amount < amount)
                return false;

            BigNumber oldAmount = entry.Amount;
            BigNumber newAmount = oldAmount - amount;

            entry.Amount = newAmount;

            NotifyChanged(currencyType, oldAmount, newAmount);
            return true;
        }
        
        private CurrencyEntry GetEntry(CurrencyType currencyType)
        {
            CurrencyEntry entry = FindEntry(currencyType);

            if (entry != null)
                return entry;

            entry = new CurrencyEntry
            {
                CurrencyType = currencyType,
                Amount = BigNumber.Zero
            };

            currencies.Add(entry);
            return entry;
        }

        private CurrencyEntry FindEntry(CurrencyType currencyType)
        {
            for (int i = 0; i < currencies.Count; i++)
            {
                if (currencies[i].CurrencyType == currencyType)
                    return currencies[i];
            }

            return null;
        }

        private bool TryParseCurrencyType(string value, out CurrencyType currencyType)
        {
            if (Enum.TryParse(value, true, out currencyType))
                return true;

            Debug.LogError($"[CurrencyManager] Unknown currency type: {value}");
            return false;
        }

        private bool TryParseAmount(string value, out BigNumber amount)
        {
            amount = BigNumber.Zero;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Ưu tiên format server nếu đã có: "mantissa|tier"
            // if (BigNumber.TryParseServerString(value, out amount))
            //     return true;

            // Fallback tạm thời: server/client gửi số thường dạng string "1200".
            if (double.TryParse(
                    value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double number))
            {
                amount = BigNumber.FromDouble(number);
                return true;
            }

            Debug.LogError($"[CurrencyManager] Cannot parse amount: {value}");
            return false;
        }

        private void NotifyChanged(CurrencyType currencyType, BigNumber oldAmount, BigNumber newAmount)
        {
            OnCurrencyChanged?.Invoke(new CurrencyChangedArgs
            {
                CurrencyType = currencyType,
                OldAmount = oldAmount,
                NewAmount = newAmount,
                Delta = newAmount - oldAmount
            });

            OnAnyCurrencyChanged?.Invoke();
        }
    }
}