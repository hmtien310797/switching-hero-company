using System;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.UI;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    public class UIWeaponFusionPopup : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;
        [SerializeField] private Button btnClose;

        [Header("Weapon Preview")]
        [SerializeField] private Image imgWeaponIcon;
        [SerializeField] private Image imgTierLabel;
        [SerializeField] private Image imgTierBackground;
        [SerializeField] private UIWeaponStarDisplay starDisplay;
        [SerializeField] private TMP_Text txtShard;
        [SerializeField] private Image shardSlider;

        [Header("Target Preview")]
        [SerializeField] private Image imgTargetUnknownIcon;

        [Header("Consumable")]
        [SerializeField] private Image imgConsumableIcon;
        [SerializeField] private TMP_Text txtConsumableAmount;

        [Header("Count Selector")]
        [SerializeField] private Button btnMin;
        [SerializeField] private Button btnDecrease;
        [SerializeField] private TMP_Text txtCount;
        [SerializeField] private Button btnIncrease;
        [SerializeField] private Button btnMax;

        [Header("Action")]
        [SerializeField] private Button btnFusion;

        [Header("Result Popup")]
        [SerializeField] private UIWeaponFuseAllResultPopup resultPopup;
        
        [Header("Colors")]
        [SerializeField] private Color enoughColor = Color.green;
        [SerializeField] private Color notEnoughColor = Color.red;

        private WeaponFusionPopupViewModel currentVm;
        private int currentCount;
        private Action onFusionCompleted;

        private void Awake()
        {
            BindStaticButtons();
            Hide();
        }

        public void Show(WeaponFusionPopupViewModel vm, Action fusionCompleted = null)
        {
            currentVm = vm;
            onFusionCompleted = fusionCompleted;
            currentCount = vm != null && vm.MaxFusionCount > 0
                ? Math.Clamp(vm.CurrentFusionCount, 1, vm.MaxFusionCount)
                : 0;

            if (root != null)
                root.SetActive(true);
            else
                gameObject.SetActive(true);

            RefreshUI();
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void BindStaticButtons()
        {
            if (btnClose != null)
            {
                btnClose.onClick.RemoveAllListeners();
                btnClose.onClick.AddListener(Hide);
            }

            if (btnMin != null)
            {
                btnMin.onClick.RemoveAllListeners();
                btnMin.onClick.AddListener(SetMin);
            }

            if (btnDecrease != null)
            {
                btnDecrease.onClick.RemoveAllListeners();
                btnDecrease.onClick.AddListener(Decrease);
            }

            if (btnIncrease != null)
            {
                btnIncrease.onClick.RemoveAllListeners();
                btnIncrease.onClick.AddListener(Increase);
            }

            if (btnMax != null)
            {
                btnMax.onClick.RemoveAllListeners();
                btnMax.onClick.AddListener(SetMax);
            }

            if (btnFusion != null)
            {
                btnFusion.onClick.RemoveAllListeners();
                btnFusion.onClick.AddListener(OnClickFusion);
            }
        }

        private void RefreshUI()
        {
            if (currentVm == null)
                return;

            BindTierVisual(currentVm.Tier);

            if (imgWeaponIcon != null)
                imgWeaponIcon.sprite = currentVm.WeaponIcon;

            if (starDisplay != null)
                starDisplay.BindStandard(currentVm.Star);

            int requiredShard = currentVm.RequiredShardPerFusion * Math.Max(1, currentCount);
            bool enoughShard = currentVm.CurrentShard >= requiredShard;

            if (txtShard != null)
            {
                txtShard.text = $"{currentVm.CurrentShard}/{requiredShard}";
                txtShard.color = enoughShard ? enoughColor : notEnoughColor;
            }

            if (shardSlider != null)
            {
                shardSlider.fillAmount = requiredShard > 0
                    ? Mathf.Clamp01(currentVm.CurrentShard / requiredShard)
                    : 0f;
            }

            if (imgConsumableIcon != null)
                imgConsumableIcon.sprite = currentVm.ConsumableCurrencyIcon;

            BigNumber requiredCurrency = currentVm.ConsumableCostPerFusion * BigNumber.Max(1, currentCount);
            bool enoughCurrency = currentVm.CurrentConsumableAmount >= requiredCurrency;

            if (txtConsumableAmount != null)
            {
                txtConsumableAmount.text = $"{currentVm.CurrentConsumableAmount}/{requiredCurrency}";
                txtConsumableAmount.color = enoughCurrency ? enoughColor : notEnoughColor;
            }

            if (txtCount != null)
                txtCount.text = BigNumber.Max(0, currentCount).ToString();

            bool canFusionNow =
                currentVm.MaxFusionCount > 0 &&
                currentCount > 0 &&
                currentCount <= currentVm.MaxFusionCount &&
                enoughShard &&
                enoughCurrency;

            if (btnFusion != null)
                btnFusion.interactable = canFusionNow;
            
            bool hasAny = currentVm.MaxFusionCount > 0;
            bool isMin = currentCount <= 1;
            bool isMax = currentCount >= currentVm.MaxFusionCount;

            if (btnMin != null)
                btnMin.interactable = hasAny && !isMin;

            if (btnDecrease != null)
                btnDecrease.interactable = hasAny && !isMin;

            if (btnIncrease != null)
                btnIncrease.interactable = hasAny && !isMax;

            if (btnMax != null)
                btnMax.interactable = hasAny && !isMax;
        }

        private void BindTierVisual(WeaponTier tier)
        {
            var entry = ItemTierVisualImageService.GetItemTierEntry(tier);
            if (entry == null)
                return;

            if (imgTierLabel != null)
                imgTierLabel.sprite = entry.tierIcon;

            if (imgTierBackground != null)
                imgTierBackground.sprite = entry.background;
        }

        private void SetMin()
        {
            if (currentVm == null || currentVm.MaxFusionCount <= 0)
                return;

            currentCount = 1;
            RefreshUI();
        }

        private void Decrease()
        {
            if (currentVm == null || currentVm.MaxFusionCount <= 0)
                return;

            currentCount = Math.Max(1, currentCount - 1);
            RefreshUI();
        }

        private void Increase()
        {
            if (currentVm == null || currentVm.MaxFusionCount <= 0)
                return;

            currentCount = Math.Min(currentVm.MaxFusionCount, currentCount + 1);
            RefreshUI();
        }

        private void SetMax()
        {
            if (currentVm == null || currentVm.MaxFusionCount <= 0)
                return;

            currentCount = currentVm.MaxFusionCount;
            RefreshUI();
        }

        private async void OnClickFusion()
        {
            if (WeaponManager.Instance == null || currentVm == null || currentCount <= 0)
                return;

            var result = await WeaponManager.Instance.TryFusionForSelectedWeaponAsync(currentVm.WeaponId, currentCount);

            if (result != null && result.HasAnyReward && resultPopup != null)
                resultPopup.Show(result);

            onFusionCompleted?.Invoke();
            Hide();
        }
    }
}