using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Repositories;
using Immortal_Switch.Scripts.Shared;

namespace Immortal_Switch.Scripts.Pvp.Services
{
    /// <summary>
    /// Phase-1 mock PvP Shop. Server chưa làm → đọc config từ
    /// <see cref="DatabaseManager.GetPvpShop"/> (bảng PvpShopInfo) và persist số lần mua local vào
    /// repository. <b>TODO server:</b> thay impl này bằng service gọi RPC (server trả
    /// refreshAtUtc + purchasedCount thật, trừ payment currency khi buy) — giữ interface
    /// <see cref="IPvPShopService"/>.
    /// </summary>
    internal sealed class LocalPvPShopService : IPvPShopService
    {
        private const int RefreshAfterDays = 7; // TODO server: server tính thời gian refresh thật.

        private readonly IPvPRepository _repo;
        private PvpShopPurchaseData _purchase;

        public LocalPvPShopService(IPvPRepository repo)
        {
            _repo = repo;
            _purchase = _repo.Load<PvpShopPurchaseData>(PvpEs3Keys.ShopPurchases);
        }

        public UniTask<PvpShopDataModel> GetShopAsync(CancellationToken token)
        {
            var rows = GetRows();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var items = new List<PvpShopItemModel>(rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                _purchase.Items.TryGetValue(row.shopItemId, out var purchased);

                items.Add(new PvpShopItemModel
                {
                    ShopItemId = row.shopItemId,
                    RewardItemId = row.rewardItemId,
                    RewardQuantity = row.rewardQuantity,
                    PaymentCurrencyId = row.paymentCurrencyId,
                    PaymentAmount = row.paymentAmount,
                    MaxPurchasePerWeek = row.maxPurchasePerWeek,
                    PurchasedCount = purchased
                });
            }

            var data = new PvpShopDataModel
            {
                ServerTimeUtc = now,
                RefreshAtUtc = now + RefreshAfterDays * 86400L,
                Items = items
            };

            return UniTask.FromResult(data);
        }

        public UniTask<PvpShopBuyResult> BuyAsync(int shopItemId, int quantity, CancellationToken token)
        {
            var row = GetRows().FirstOrDefault(r => r.shopItemId == shopItemId);
            if (row == null)
            {
                return UniTask.FromResult(new PvpShopBuyResult
                {
                    Success = false,
                    Error = $"[PvP Shop] Không tồn tại item shopItemId={shopItemId}."
                });
            }

            _purchase.Items.TryGetValue(shopItemId, out var purchased);
            if (quantity <= 0 || purchased + quantity > row.maxPurchasePerWeek)
            {
                return UniTask.FromResult(new PvpShopBuyResult
                {
                    Success = false,
                    Error = "[PvP Shop] Đã đạt giới hạn mua trong tuần."
                });
            }

            // Phase-1 local: chỉ đánh dấu đã mua + trả reward. TODO server: trừ payment currency,
            // kiểm duyệt số dư, trả purchasedCount mới từ server.
            _purchase.Items[shopItemId] = purchased + quantity;
            _repo.Save(PvpEs3Keys.ShopPurchases, _purchase);

            var rewards = new List<ItemData> { new ItemData(row.rewardItemId, row.rewardQuantity * quantity) };
            return UniTask.FromResult(new PvpShopBuyResult { Success = true, Rewards = rewards });
        }

        private List<DynamicHeroesGlobalSpecificationsPvpShopInfoRow> GetRows()
        {
            var dbm = DatabaseManager.Instance;
            return dbm != null
                ? dbm.GetPvpShop()
                : new List<DynamicHeroesGlobalSpecificationsPvpShopInfoRow>();
        }
    }
}