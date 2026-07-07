using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.PlayerSystem;
using Immortal_Switch.Scripts.Shop.Interfaces;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shop
{
    public class ShopManager : Singleton<ShopManager>
    {
        public IShopService Service { get; private set; }
        public IShopStorage Storage { get; private set; }

        /// <summary>Fire khi purchase count thay đổi (mua mới hoặc reset).</summary>
        public event Action OnDataChanged;

        protected override void OnSingletonAwake()
        {
            Load();
        }

        public override UniTask InitializeAsync()
        {
            Service.CheckAndReset();
            SubscribeEvents();
            return UniTask.CompletedTask;
        }

        protected override void OnDestroy()
        {
            UnsubscribeEvents();
            base.OnDestroy();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void RecordPurchase(int packId)
        {
            Service.RecordPurchase(packId);
            OnDataChanged?.Invoke();
        }

        public int GetRemaining(int packId, int limit)
        {
            return Service.GetRemaining(packId, limit);
        }

        public int GetPurchasedCount(int packId)
        {
            return Service.GetPurchasedCount(packId);
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void Load()
        {
            Storage = new ShopStorage();
            Service = new ShopService(Storage);
            Storage.Load();
        }

        private void SubscribeEvents()
        {
            PlayerSystemManager.Instance.OnLoginNewDay += OnLoginNewDay;
        }

        private void UnsubscribeEvents()
        {
            PlayerSystemManager.Instance.OnLoginNewDay -= OnLoginNewDay;
        }

        private void OnLoginNewDay()
        {
            Service.CheckAndReset();
            OnDataChanged?.Invoke();
        }
    }
}