using System;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PvpShopItem : MonoBehaviour
{
    // -- Các field dưới đây wire sẵn trong prefab PvpShopItem.prefab --
    // pvpItemImage:     icon vật phẩm nhận được (reward_item_id)
    // pvpItemToPay:     icon tiền tệ thanh toán (payment_currency_id)
    // textPvpItemName:  tên vật phẩm (từ item_config)
    // textPvpMaxLimitBuyQuantity: format {0}/{1}, {0} = số lần đã mua trong tuần (server),
    //                             {1} = maxPurchaseTimePerWeek (bảng PvpShopInfo)
    // textPvpQuantityPerBuy: số lượng nhận được mỗi lần mua (reward_quantity)
    // textPvpPrice:          giá bán (payment_amount)
    // btnBuy:          nút mua — prefab hiện gắn vào "ButtonInfo"; designer có thể đổi sang phần tử khác.
    [SerializeField] private Image pvpItemImage;
    [SerializeField] private Image pvpItemToPay;
    [SerializeField] private TMP_Text textPvpItemName;
    [SerializeField] private TMP_Text textPvpMaxLimitBuyQuantity;
    [SerializeField] private TMP_Text textPvpQuantityPerBuy;
    [SerializeField] private TMP_Text textPvpPrice;
    [SerializeField] private Button btnBuy;
    [SerializeField] private Button btnInfo;

    // --- Private Fields ---
    private Action<PvpShopItemModel> _onClickBuy;
    private PvpShopItemModel _data;

    private void Awake()
    {
        if (btnBuy != null)
        {
            btnBuy.onClick.AddListener(OnClickBuy);
        }
    }

    private void OnDestroy()
    {
        if (btnBuy != null)
        {
            btnBuy.onClick.RemoveListener(OnClickBuy);
        }
    }

    private void OnClickBuy()
    {
        _onClickBuy?.Invoke(_data);
    }

    /// <summary>
    /// Bind một item PvP shop. <paramref name="item"/> là model server (chứa cả config lẫn
    /// <see cref="PvpShopItemModel.PurchasedCount"/> — {0} của text limit). <b>TODO server:</b>
    /// purchasedCount server trả; nút buy mặc định gắn vào "ButtonInfo".
    /// </summary>
    public void Bind(PvpShopItemModel item, Action<PvpShopItemModel> onClickBuy)
    {
        if (item == null)
        {
            Debug.LogWarning("[PvpShopItem] Bind với item null — bỏ qua.", this);
            return;
        }

        _data = item;
        _onClickBuy = onClickBuy;

        var database = DatabaseManager.Instance;

        if (pvpItemImage != null)
        {
            var rewardDisplay = database?.GetDisplayData(item.RewardItemId);
            if (rewardDisplay?.ItemIcon != null)
            {
                pvpItemImage.sprite = rewardDisplay.ItemIcon;
            }
        }

        if (pvpItemToPay != null)
        {
            var payDisplay = database?.GetDisplayData(item.PaymentCurrencyId);
            if (payDisplay?.ItemIcon != null)
            {
                pvpItemToPay.sprite = payDisplay.ItemIcon;
            }
        }

        if (textPvpItemName != null)
        {
            var cfg = database?.ItemDb?.FindItem(item.RewardItemId);
            textPvpItemName.text = cfg != null ? LocalizationManager.GetText(cfg.itemName) : $"Item {item.RewardItemId}";
        }

        if (textPvpMaxLimitBuyQuantity != null)
        {
            textPvpMaxLimitBuyQuantity.text = $"{item.PurchasedCount}/{item.MaxPurchasePerWeek}";
        }

        if (textPvpQuantityPerBuy != null)
        {
            textPvpQuantityPerBuy.text = $"x{item.RewardQuantity}";
        }

        if (textPvpPrice != null)
        {
            textPvpPrice.text = item.PaymentAmount.ToString();
        }

        // Khi đã mua đủ giới hạn tuần thì khoá nút mua (mock local — server thật sẽ lo trừ tiền).
        if (btnBuy != null)
        {
            
        }
    }
}