using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    public class UIWeaponUpgradePanel : MonoBehaviour
    {
        [Header("Shared Preview")]
        [SerializeField] private TMP_Text txtWeaponName;
        [SerializeField] private Image imgWeaponIcon;
        [SerializeField] private TMP_Text txtShard;
        [SerializeField] private TMP_Text txtStar;
        [SerializeField] private TMP_Text txtLevelRange;
        [SerializeField] private Slider shardSlider;

        [Header("Mode Roots")]
        [SerializeField] private GameObject upgradeModeRoot;
        [SerializeField] private GameObject limitBreakModeRoot;

        [Header("Upgrade Mode")]
        [SerializeField] private Transform statPreviewContainer;
        [SerializeField] private UIWeaponUpgradeStatLineItem statPreviewItemPrefab;
        [SerializeField] private TMP_Text txtNextLevelCost;
        [SerializeField] private TMP_Text txtLevelUpAllCost;
        [SerializeField] private Button btnLevelUp;
        [SerializeField] private Button btnLevelUpAll;

        [Header("Limit Break Mode")]
        [SerializeField] private TMP_Text txtCurrentMaxLevel;
        [SerializeField] private TMP_Text txtNextMaxLevel;
        [SerializeField] private TMP_Text txtBreakCost;
        [SerializeField] private TMP_Text txtBreakRate;
        [SerializeField] private TMP_Text txtNextBreakRequiredLevel;
        [SerializeField] private Button btnLimitBreak;

        [Header("Close")]
        [SerializeField] private Button btnClose;

        private readonly List<UIWeaponUpgradeStatLineItem> statPreviewItems = new();

        private WeaponUpgradePanelViewModel currentVm;
        private WeaponDetailViewModel currentDetailVm;
        private int currentHeroId;
        private Action onChanged;

        public void Bind(
            WeaponUpgradePanelViewModel vm,
            WeaponDetailViewModel detailVm,
            int heroId,
            Action changedCallback = null)
        {
            currentVm = vm;
            currentDetailVm = detailVm;
            currentHeroId = heroId;
            onChanged = changedCallback;

            BindSharedPreview(vm);
            BindModes(vm);
            BindButtons();
        }

        private void BindSharedPreview(WeaponUpgradePanelViewModel vm)
        {
            if (txtWeaponName != null)
                txtWeaponName.text = vm.WeaponName;

            if (imgWeaponIcon != null)
                imgWeaponIcon.sprite = vm.Icon;

            if (txtShard != null)
            {
                txtShard.text = vm.MaxShard > 0
                    ? $"{vm.CurrentShard}/{vm.MaxShard}"
                    : vm.CurrentShard.ToString();
            }

            if (txtStar != null)
                txtStar.text = $"{vm.CurrentStar}/{vm.MaxStar}";

            if (txtLevelRange != null)
                txtLevelRange.text = $"{vm.CurrentLevel}/{vm.CurrentMaxLevel}";
            
            if (txtShard != null)
            {
                txtShard.text = vm.MaxShard > 0
                    ? $"{vm.CurrentShard}/{vm.MaxShard}"
                    : vm.CurrentShard.ToString();
            }

            if (shardSlider != null)
            {
                shardSlider.minValue = 0f;
                shardSlider.maxValue = 1f;
                shardSlider.value = vm.ShardProgressNormalized;
            }
        }

        private void BindModes(WeaponUpgradePanelViewModel vm)
        {
            if (upgradeModeRoot != null)
                upgradeModeRoot.SetActive(vm.ShowUpgradeMode);

            if (limitBreakModeRoot != null)
                limitBreakModeRoot.SetActive(vm.ShowLimitBreakMode);

            if (vm.ShowUpgradeMode)
                BindUpgradeMode(vm);

            if (vm.ShowLimitBreakMode)
                BindLimitBreakMode(vm);
        }

        private void BindUpgradeMode(WeaponUpgradePanelViewModel vm)
        {
            EnsureStatPreviewPool(vm.StatPreviewLines.Count);

            for (int i = 0; i < statPreviewItems.Count; i++)
            {
                bool active = i < vm.StatPreviewLines.Count;
                statPreviewItems[i].gameObject.SetActive(active);

                if (!active)
                    continue;

                var line = vm.StatPreviewLines[i];
                statPreviewItems[i].Bind(line.StatName, line.CurrentValueText, line.NextValueText);
            }

            if (txtNextLevelCost != null)
                txtNextLevelCost.text = vm.NextLevelCost.ToString();

            if (txtLevelUpAllCost != null)
                txtLevelUpAllCost.text = vm.LevelUpAllCost.ToString();

            if (btnLevelUp != null)
            {
                btnLevelUp.gameObject.SetActive(vm.ShowLevelUp);
                btnLevelUp.interactable = vm.CanLevelUp;
            }

            if (btnLevelUpAll != null)
            {
                btnLevelUpAll.gameObject.SetActive(vm.ShowLevelUpAll);
                btnLevelUpAll.interactable = vm.CanLevelUpAll;
            }
        }

        private void BindLimitBreakMode(WeaponUpgradePanelViewModel vm)
        {
            if (txtCurrentMaxLevel != null)
                txtCurrentMaxLevel.text = vm.CurrentMaxLevel.ToString();

            if (txtNextMaxLevel != null)
                txtNextMaxLevel.text = vm.NextMaxLevel.ToString();

            if (txtBreakCost != null)
                txtBreakCost.text = vm.BreakThroughCost.ToString();

            if (txtBreakRate != null)
                txtBreakRate.text = $"{vm.LimitBreakSuccessRate * 100f:0.##}%";

            if (txtNextBreakRequiredLevel != null)
                txtNextBreakRequiredLevel.text = vm.NextBreakRequiredLevel.ToString();

            if (btnLimitBreak != null)
            {
                btnLimitBreak.gameObject.SetActive(vm.ShowLimitBreak);
                btnLimitBreak.interactable = vm.CanLimitBreak;
            }
        }

        private void BindButtons()
        {
            if (btnLevelUp != null)
            {
                btnLevelUp.onClick.RemoveAllListeners();
                btnLevelUp.onClick.AddListener(OnClickLevelUp);
            }

            if (btnLevelUpAll != null)
            {
                btnLevelUpAll.onClick.RemoveAllListeners();
                btnLevelUpAll.onClick.AddListener(OnClickLevelUpAll);
            }

            if (btnLimitBreak != null)
            {
                btnLimitBreak.onClick.RemoveAllListeners();
                btnLimitBreak.onClick.AddListener(OnClickLimitBreak);
            }

            if (btnClose != null)
            {
                btnClose.onClick.RemoveAllListeners();
                btnClose.onClick.AddListener(OnClickClose);
            }
        }

        private void EnsureStatPreviewPool(int targetCount)
        {
            while (statPreviewItems.Count < targetCount)
            {
                var item = Instantiate(statPreviewItemPrefab, statPreviewContainer);
                statPreviewItems.Add(item);
            }
        }

        private void OnClickLevelUp()
        {
            if (WeaponManager.Instance == null || currentDetailVm == null)
                return;

            if (currentDetailVm.IsExclusive)
                WeaponManager.Instance.TryLevelUpExclusive(currentHeroId);
            else
                WeaponManager.Instance.TryLevelUpStandard(currentDetailVm.WeaponId);

            onChanged?.Invoke();
        }

        private void OnClickLevelUpAll()
        {
            if (WeaponManager.Instance == null || currentDetailVm == null)
                return;

            if (currentDetailVm.IsExclusive)
            {
                while (WeaponManager.Instance.TryLevelUpExclusive(currentHeroId, false)) { }
                WeaponManager.Instance.Save();
                WeaponManager.Instance.NotifyHeroWeaponChanged(currentHeroId);
            }
            else
            {
                while (WeaponManager.Instance.TryLevelUpStandard(currentDetailVm.WeaponId, false)) { }
                WeaponManager.Instance.Save();
                WeaponManager.Instance.NotifyStandardWeaponChanged(currentDetailVm.WeaponId);
            }

            onChanged?.Invoke();
        }

        private void OnClickLimitBreak()
        {
            if (WeaponManager.Instance == null || currentDetailVm == null)
                return;

            if (currentDetailVm.IsExclusive)
                WeaponManager.Instance.TryLimitBreakExclusive(currentHeroId);
            else
                WeaponManager.Instance.TryLimitBreakStandard(currentDetailVm.WeaponId);

            onChanged?.Invoke();
        }

        private void OnClickClose()
        {
            gameObject.SetActive(false);
        }
    }
}