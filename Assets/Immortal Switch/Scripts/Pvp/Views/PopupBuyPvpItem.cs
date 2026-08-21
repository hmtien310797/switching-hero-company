using System;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupBuyPvpItem : MonoBehaviour
{
    [SerializeField] private UIRewardQuantity uiReward;
    [SerializeField] private TMP_Text textPvpItemName;
    [SerializeField] private Button btnPlus;
    [SerializeField] private Button btnMinus;
    [SerializeField] private Button btnMax;
    [SerializeField] private Button btnMin;
    [SerializeField] private Slider slider;
    [SerializeField] private Image imagePvpItemToPay;
    [SerializeField] private TMP_Text textPvpItemPrice;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button closeButton;

    // --- Private Fields ---
    private PvpShopItemModel _data;
    private Action<int> _onBuy;

    /// <summary>Số lượng đang chọn (clamp trong [1, _maxQty]).</summary>
    private int _currentQty = 1;

    /// <summary>Cận trên = số lần còn lại trong tuần: maxPurchasePerWeek - purchasedCount.</summary>
    private int _maxQty = 1;

    /// <summary>Chặn vòng lặp khi gán slider.value bằng code.</summary>
    private bool _syncingUi;

    private void OnEnable()
    {
        if (btnPlus != null) btnPlus.onClick.AddListener(Increase);
        if (btnMinus != null) btnMinus.onClick.AddListener(Decrease);
        if (btnMax != null) btnMax.onClick.AddListener(SetMax);
        if (btnMin != null) btnMin.onClick.AddListener(SetMin);
        if (buyButton != null) buyButton.onClick.AddListener(ConfirmBuy);
        if (slider != null) slider.onValueChanged.AddListener(OnSliderChanged);
        if(closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        if (btnPlus != null) btnPlus.onClick.RemoveListener(Increase);
        if (btnMinus != null) btnMinus.onClick.RemoveListener(Decrease);
        if (btnMax != null) btnMax.onClick.RemoveListener(SetMax);
        if (btnMin != null) btnMin.onClick.RemoveListener(SetMin);
        if (buyButton != null) buyButton.onClick.RemoveListener(ConfirmBuy);
        if (slider != null) slider.onValueChanged.RemoveListener(OnSliderChanged);
        if(closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    /// <summary>
    /// Mở popup cho một item. <paramref name="onBuy"/> nhận số lượng đã chọn; view sẽ gọi
    /// <c>BuyAsync(item, qty)</c> và tự đóng popup khi thành công.
    /// </summary>
    public void Open(PvpShopItemModel item, Action<int> onBuy)
    {
        _data = item;
        _onBuy = onBuy;

        // Cận trên = số lần mua còn lại trong tuần; tối thiểu 1 để luôn mua được 1.
        _maxQty = item != null ? Math.Max(1, item.MaxPurchasePerWeek - item.PurchasedCount) : 1;
        _currentQty = 1;

        if (slider != null)
        {
            slider.minValue = 1;
            slider.maxValue = _maxQty;
            slider.wholeNumbers = true;
        }

        Render();
    }

    private void SetMin() => SetQty(1);
    private void SetMax() => SetQty(_maxQty);
    private void Decrease() => SetQty(_currentQty - 1);
    private void Increase() => SetQty(_currentQty + 1);
    private void Close () => this.gameObject.SetActive(false);

    private void OnSliderChanged(float value)
    {
        if (_syncingUi) return;
        SetQty(Mathf.RoundToInt(value));
    }

    private void SetQty(int qty)
    {
        _currentQty = Mathf.Clamp(qty, 1, Mathf.Max(1, _maxQty));
        Render();
    }

    private void Render()
    {
        if (_data == null) return;

        var database = DatabaseManager.Instance;

        if (textPvpItemName != null)
        {
            var cfg = database?.ItemDb?.FindItem(_data.RewardItemId);
            textPvpItemName.text = cfg != null ? LocalizationManager.GetText(cfg.itemName) : $"Item {_data.RewardItemId}";
        }

        if (uiReward != null)
        {
            // Hiển thị reward icon + tổng số lượng nhận được.
            uiReward.Bind(_data.RewardItemId, _data.RewardQuantity * _currentQty);
        }

        if (imagePvpItemToPay != null)
        {
            var payDisplay = database?.GetDisplayData(_data.PaymentCurrencyId);
            if (payDisplay?.ItemIcon != null)
            {
                imagePvpItemToPay.sprite = payDisplay.ItemIcon;
            }
        }

        if (textPvpItemPrice != null)
        {
            // Tổng giá = giá một lần × số lượng.
            textPvpItemPrice.text = (_data.PaymentAmount * _currentQty).ToString();
        }

        if (btnMinus != null) btnMinus.interactable = _currentQty > 1;
        if (btnMin != null) btnMin.interactable = _currentQty > 1;
        if (btnPlus != null) btnPlus.interactable = _currentQty < _maxQty;
        if (btnMax != null) btnMax.interactable = _currentQty < _maxQty;

        if (slider != null)
        {
            _syncingUi = true;
            slider.value = _currentQty;
            _syncingUi = false;
        }
    }

    private void ConfirmBuy()
    {
        if (_data == null || _currentQty <= 0) return;
        _onBuy?.Invoke(_currentQty);
    }
}