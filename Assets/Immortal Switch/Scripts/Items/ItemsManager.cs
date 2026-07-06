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
                _runtime.Items.Add(new ItemData
                {
                    ItemKey = entry.ItemId,
                    Quantity = entry.Quantity,
                });
            }
        }

        public List<ItemData> GetItems()
        {
            return _runtime.Items;
        }
    }
}