using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PvpShopUiView : AnimatedUIView
{
    // Remaining time cho tới khi toàn bộ shop item refresh — server trả refreshAtUtc.
    [SerializeField] private TMP_Text textRefreshTime;
    [SerializeField] private PvpShopItem pvpShopItemPrefab;
    [SerializeField] private Transform itemContainer;   // Content của ScrollView (GridLayoutGroup)
    [SerializeField] private PopupBuyPvpItem popupBuyPvpItem;

    // --- Private Fields ---
    private readonly List<PvpShopItem> _items = new();
    private PvpShopDataModel _data;

    public override void OnShow(object args)
    {
        base.OnShow(args);
        LoadShopAsync().Forget();
    }

    public override void OnHide()
    {
        popupBuyPvpItem.gameObject.SetActive(false);
        base.OnHide();
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy || textRefreshTime == null || _data == null || _data.RefreshAtUtc <= 0)
        {
            return;
        }

        var remain = _data.RefreshAtUtc - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        textRefreshTime.text = "Next refresh: " + FormatCountdown(remain);
    }

    private async UniTaskVoid LoadShopAsync()
    {
        var shop = PvpManager.Instance?.Facade?.Shop;
        if (shop == null) return;

        try
        {
            _data = await shop.GetShopAsync(System.Threading.CancellationToken.None);
            RenderItems(_data);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PvP] Load PvP Shop failed: {e.Message}");
        }
    }

    private void RenderItems(PvpShopDataModel data)
    {
        if (data?.Items == null) return;

        var count = data.Items.Count;
        for (var i = 0; i < count; i++)
        {
            var item = data.Items[i];

            PvpShopItem clone;
            if (i < _items.Count)
            {
                clone = _items[i];
                clone.gameObject.SetActive(true);
            }
            else
            {
                if (pvpShopItemPrefab == null || itemContainer == null)
                {
                    Debug.LogWarning("[PvP] PvpShopUiView: thiếu pvpShopItemPrefab hoặc itemContainer — bỏ qua list shop.");
                    return;
                }

                clone = Instantiate(pvpShopItemPrefab, itemContainer, false);
                _items.Add(clone);
            }

            clone.Bind(item, OnBuy);
        }

        // Tắt những item dư nếu config bớt đi.
        for (var i = count; i < _items.Count; i++)
        {
            _items[i].gameObject.SetActive(false);
        }
    }

    private void OnBuy(PvpShopItemModel item)
    {
        // Bật popup chọn số lượng; modà mua khi user xác nhận. Nếu chưa wire popup thì mua nhanh 1 lần.
        if (popupBuyPvpItem != null)
        {
            popupBuyPvpItem.gameObject.SetActive(true);
            popupBuyPvpItem.Open(item, qty => BuyAsync(item, qty));
        }
        else
        {
            BuyAsync(item, 1);
        }
    }

    private async UniTaskVoid BuyAsync(PvpShopItemModel item, int quantity)
    {
        var shop = PvpManager.Instance?.Facade?.Shop;
        if (shop == null) return;

        try
        {
            var result = await shop.BuyAsync(item.ShopItemId, quantity, System.Threading.CancellationToken.None);

            if (!result.Success)
            {
                Debug.LogWarning($"[PvP] Buy item {item.ShopItemId} failed: {result.Error}");
                Toast(result.Error ?? "Mua thất bại.");
                // Giữ popup mở để user chỉnh lại số lượng.
                return;
            }

            // Mua xong — đóng popup trước khi hiện phần thưởng.
            if (popupBuyPvpItem != null)
            {
                popupBuyPvpItem.gameObject.SetActive(false);
            }

            if (result.Rewards != null && result.Rewards.Count > 0)
            {
                PopupRewardService.Show(result.Rewards);
            }

            // Load lại để cập nhật purchasedCount ({0} của limit).
            LoadShopAsync().Forget();
        }
        catch (Exception e)
        {
            Debug.LogError($"[PvP] Buy item {item.ShopItemId} exception: {e.Message}");
        }
    }

    private void OnClickClose()
    {
        UIManager.Instance.Close<PvpShopUiView>();
    }

    private static string FormatCountdown(long seconds)
    {
        if (seconds < 0) seconds = 0;
        var d = seconds / 86400;
        var h = (seconds % 86400) / 3600;
        var m = (seconds % 3600) / 60;
        var s = seconds % 60;
        return $"{d}d {h:D2}:{m:D2}:{s:D2}";
    }

    private static void Toast(string message)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast(message);
        }
    }
}