using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Items.Models;
using UnityEngine;

namespace Immortal_Switch.Scripts.Items
{
    public class ItemsManager : Singleton<ItemsManager>
    {
        private readonly ItemsRuntime _runtime = new();

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        public void SyncFromReward(IReadOnlyList<RewardDto> rewards)
        {
            if (rewards == null)
            {
                return;
            }

            foreach (var b in rewards)
            {
                if (Enum.TryParse(b.CurrencyType, true, out ECurrencyType type) &&
                    TryParseAmount(b.Amount, out var amount))
                {
                    SetQuantity(type, amount);
                }
            }
        }

        private bool TryParseAmount(string value, out BigNumber amount)
        {
            amount = BigNumber.Zero;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

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

            Debug.LogError($"[ItemsManager] Cannot parse amount: {value}");
            return false;
        }

        public void SyncFromServer(BagResponse rsp)
        {
            if (rsp == null ||
                _runtime == null)
            {
                Debug.LogError("[ItemsManager]: Sync returned null");
                return;
            }

            if (_runtime.Items.Count > 0)
            {
                _runtime.Items.Clear();
            }

            foreach (var entry in rsp.Items)
            {
                if (int.TryParse(entry.ItemId, out var itemId))
                {
                    _runtime.Items[itemId] = new ItemData(itemId, entry.Quantity);
                }
            }
        }

        public void SetQuantity(ECurrencyType currencyType, BigNumber quantity)
        {
            foreach (var item in _runtime.Items)
            {
                if (item.Key == (int)currencyType)
                {
                    item.Value.Quantity = quantity;
                    return;
                }
            }
        }

        public BigNumber GetQuantity(int itemId)
        {
            foreach (var item in _runtime.Items)
            {
                if (item.Key == itemId)
                {
                    return item.Value.Quantity;
                }
            }

            return 0;
        }

        public BigNumber GetQuantity(ECurrencyType itemId)
        {
            return GetQuantity((int)itemId);
        }

        public Dictionary<int, ItemData> GetAllItem()
        {
            return _runtime.Items
                .Where(v => v.Value != null && v.Value.Quantity > 0)
                .ToDictionary(v => v.Key, v => v.Value);
        }
    }
}