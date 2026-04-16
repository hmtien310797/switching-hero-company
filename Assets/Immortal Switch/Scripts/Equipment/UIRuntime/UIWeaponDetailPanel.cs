using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    public class UIWeaponDetailPanel : MonoBehaviour
    {
        [Header("Top Info")]
        [SerializeField] private TMP_Text txtWeaponName;
        [SerializeField] private Image imgIcon;
        [SerializeField] private TMP_Text txtTierOrStar;
        [SerializeField] private TMP_Text txtLevel;

        [Header("Equip Effect")]
        [SerializeField] private Transform statLineContainer;
        [SerializeField] private UIWeaponStatLineItem statLinePrefab;

        [Header("Buttons")]
        [SerializeField] private Button btnEquip;
        [SerializeField] private Button btnLevelUp;
        [SerializeField] private Button btnLevelUpAll;
        [SerializeField] private Button btnLimitBreak;
        [SerializeField] private Button btnFuse;

        [Header("Upgrade Panel")]
        [SerializeField] private UIWeaponUpgradePanel upgradePanel;

        private readonly List<UIWeaponStatLineItem> statLineItems = new();

        private WeaponDetailViewModel currentVm;
        private int currentHeroId;
        private Action onRequestRefresh;

        public void Bind(WeaponDetailViewModel vm, int heroId, Action refreshCallback = null)
        {
            currentVm = vm;
            currentHeroId = heroId;
            onRequestRefresh = refreshCallback;

            if (txtWeaponName != null)
                txtWeaponName.text = vm.WeaponName;

            if (imgIcon != null)
                imgIcon.sprite = vm.Icon;

            if (txtTierOrStar != null)
            {
                txtTierOrStar.text = vm.IsExclusive
                    ? $"Star {vm.Star}/{vm.MaxStar}"
                    : $"{vm.Tier}{vm.Star}";
            }

            if (txtLevel != null)
                txtLevel.text = $"Lv.{vm.Level}/{vm.CurrentMaxLevel}";

            BindStatLines(vm.EquipEffects);

            if (upgradePanel != null)
                upgradePanel.Bind(vm.UpgradePanel);

            BindButtons(vm);
        }

        private void BindStatLines(List<WeaponStatLineViewModel> stats)
        {
            EnsureStatLinePool(stats.Count);

            for (int i = 0; i < statLineItems.Count; i++)
            {
                bool active = i < stats.Count;
                statLineItems[i].gameObject.SetActive(active);

                if (!active)
                    continue;

                statLineItems[i].Bind(stats[i]);
            }
        }

        private void EnsureStatLinePool(int targetCount)
        {
            while (statLineItems.Count < targetCount)
            {
                var item = Instantiate(statLinePrefab, statLineContainer);
                statLineItems.Add(item);
            }
        }

        private void BindButtons(WeaponDetailViewModel vm)
        {
            if (btnEquip != null)
            {
                btnEquip.gameObject.SetActive(vm.CanEquip);
                btnEquip.onClick.RemoveAllListeners();
                btnEquip.onClick.AddListener(OnClickEquip);
            }

            if (btnLevelUp != null)
            {
                btnLevelUp.gameObject.SetActive(vm.UpgradePanel.ShowLevelUp);
                btnLevelUp.interactable = vm.UpgradePanel.CanLevelUp;
                btnLevelUp.onClick.RemoveAllListeners();
                btnLevelUp.onClick.AddListener(OnClickLevelUp);
            }

            if (btnLevelUpAll != null)
            {
                btnLevelUpAll.gameObject.SetActive(vm.UpgradePanel.ShowLevelUpAll);
                btnLevelUpAll.interactable = vm.UpgradePanel.CanLevelUpAll;
                btnLevelUpAll.onClick.RemoveAllListeners();
                btnLevelUpAll.onClick.AddListener(OnClickLevelUpAll);
            }

            if (btnLimitBreak != null)
            {
                btnLimitBreak.gameObject.SetActive(vm.UpgradePanel.ShowLimitBreak);
                btnLimitBreak.interactable = vm.UpgradePanel.CanLimitBreak;
                btnLimitBreak.onClick.RemoveAllListeners();
                btnLimitBreak.onClick.AddListener(OnClickLimitBreak);
            }

            if (btnFuse != null)
            {
                btnFuse.gameObject.SetActive(!vm.IsExclusive);
                btnFuse.interactable = vm.CanFuse;
                btnFuse.onClick.RemoveAllListeners();
                btnFuse.onClick.AddListener(OnClickFuse);
            }
        }

        private void OnClickEquip()
        {
            if (WeaponManager.Instance == null || currentVm == null)
                return;

            if (currentVm.IsExclusive)
                WeaponManager.Instance.EquipExclusive(currentHeroId);
            else
                WeaponManager.Instance.EquipStandard(currentHeroId, currentVm.HeroClass, currentVm.WeaponId);

            RequestRefresh();
        }

        private void OnClickLevelUp()
        {
            if (WeaponManager.Instance == null || currentVm == null)
                return;

            if (currentVm.IsExclusive)
                WeaponManager.Instance.TryLevelUpExclusive(currentHeroId);
            else
                WeaponManager.Instance.TryLevelUpStandard(currentVm.WeaponId);

            RequestRefresh();
        }

        private void OnClickLevelUpAll()
        {
            if (WeaponManager.Instance == null || currentVm == null)
                return;

            if (currentVm.IsExclusive)
            {
                while (WeaponManager.Instance.TryLevelUpExclusive(currentHeroId, false)) { }

                WeaponManager.Instance.Save();
                WeaponManager.Instance.NotifyHeroWeaponChanged(currentHeroId);
            }
            else
            {
                while (WeaponManager.Instance.TryLevelUpStandard(currentVm.WeaponId, false)) { }

                WeaponManager.Instance.Save();
                WeaponManager.Instance.NotifyStandardWeaponChanged(currentVm.WeaponId);
            }

            RequestRefresh();
        }

        private void OnClickLimitBreak()
        {
            if (WeaponManager.Instance == null || currentVm == null)
                return;

            if (currentVm.IsExclusive)
                WeaponManager.Instance.TryLimitBreakExclusive(currentHeroId);
            else
                WeaponManager.Instance.TryLimitBreakStandard(currentVm.WeaponId);

            RequestRefresh();
        }

        private void OnClickFuse()
        {
            if (WeaponManager.Instance == null || currentVm == null || currentVm.IsExclusive)
                return;

            WeaponManager.Instance.TryFuseStandard(currentVm.WeaponId);
            RequestRefresh();
        }

        private void RequestRefresh()
        {
            onRequestRefresh?.Invoke();
        }
    }
}