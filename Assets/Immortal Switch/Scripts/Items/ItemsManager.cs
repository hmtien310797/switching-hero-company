using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
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

        public void SyncFromServer(BagResponse rsp)
        {
            if (rsp == null)
            {
                Debug.LogError("[ItemsManager]: Sync returned null");
                return;
            }

            foreach (var entry in rsp.Items)
            {
                if (int.TryParse(entry.ItemId, out var itemId))
                {
                    _runtime.Items[itemId] = new ItemData(itemId, entry.Quantity);
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

        public Dictionary<int, ItemData> GetAllItem()
        {
            return _runtime.Items;
        }
    }
}