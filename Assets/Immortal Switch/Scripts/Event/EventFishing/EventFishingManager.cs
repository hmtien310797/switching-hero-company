using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Items.Models;
using Nakama;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventFishing
{
    /// <summary>
    /// Cache trạng thái Event Fishing lấy từ server (RPC eventfishing/*) — server là nguồn sự
    /// thật duy nhất, thay cho EventFishingService/Storage (ES3) cục bộ trước đây.
    /// </summary>
    public class EventFishingManager : Singleton<EventFishingManager>
    {
        /// <summary>Phát ra khi State được refresh từ server.</summary>
        public event Action OnDataChanged;

        public EventFishingStateResponse State { get; private set; }

        public override UniTask InitializeAsync() => UniTask.CompletedTask;

        /// <summary>Tải lại toàn bộ state từ server. Gọi khi mở EventFishingView và sau mỗi
        /// cast/shop_buy thành công để đồng bộ bộ sưu tập/số dư.</summary>
        public async UniTask RefreshAsync()
        {
            try
            {
                State = await NakamaClient.Instance.EventFishingStateAsync();
                OnDataChanged?.Invoke();
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventFishingManager] eventfishing/state failed: {ex.StatusCode} {ex.Message}");
            }
        }

        // ── Read-only accessors — luôn đọc từ snapshot RefreshAsync() gần nhất ──────────────

        public int CaughtCount => State?.Progress?.CaughtCount ?? 0;

        /// <summary>Kiểm tra người chơi đã từng nhận cá có ID tương ứng hay chưa.</summary>
        public bool HasCaughtFish(int collectionId)
        {
            var collection = State?.Collection;

            if (collection == null)
            {
                return false;
            }

            for (var i = 0; i < collection.Count; i++)
            {
                if (collection[i].CollectionId == collectionId)
                {
                    return collection[i].Caught;
                }
            }

            return false;
        }

        // ── Mutating actions — gọi RPC, refresh cache, trả reward cho UI hiển thị popup ─────

        /// <summary>Trừ 1 mồi, server tự random thắng/thua và random có trọng số con cá nếu
        /// thắng. Trả (success, result: "win"|"lose", fish, isFirstCatch, error).</summary>
        public async UniTask<(bool success, string result, EventFishingFishDto fish, bool isFirstCatch, string error)> CastAsync()
        {
            EventFishingCastResponse response;

            try
            {
                response = await NakamaClient.Instance.EventFishingCastAsync();
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventFishingManager] eventfishing/cast error {ex.StatusCode}: {ex.Message}");
                return (false, null, null, false, "NETWORK_ERROR");
            }

            if (!response.Success)
            {
                return (false, null, null, false, response.Error);
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            await RefreshAsync();

            return (true, response.Result, response.Fish, response.IsFirstCatch, null);
        }

        /// <summary>Mua 1 item trong shop sự kiện câu cá — is_premium=false trừ gold, true trừ diamond, cả 2 đều cấp fishing_food.</summary>
        public async UniTask<(bool success, List<ItemData> rewards, string error)> ShopBuyAsync(int shopId)
        {
            EventFishingShopBuyResponse response;

            try
            {
                response = await NakamaClient.Instance.EventFishingShopBuyAsync(
                    new EventFishingShopBuyRequest { ShopId = shopId }
                );
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventFishingManager] eventfishing/shop_buy error {ex.StatusCode}: {ex.Message}");
                return (false, new List<ItemData>(), "NETWORK_ERROR");
            }

            if (!response.Success)
            {
                return (false, new List<ItemData>(), response.Error);
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            await RefreshAsync();

            var rewards = response.Item != null
                ? new List<ItemData> { new ItemData(response.Item.ItemId, response.Item.Amount) }
                : new List<ItemData>();

            return (true, rewards, null);
        }
    }
}
