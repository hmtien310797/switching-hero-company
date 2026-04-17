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

        [Header("Action Buttons")]
        [SerializeField] private Button btnEquip;
        [SerializeField] private Button btnAutoEquip;
        [SerializeField] private Button btnOpenUpgrade;
        [SerializeField] private Button btnFusion;
        [SerializeField] private Button btnFuseAll;

        [Header("Upgrade Panel")]
        [SerializeField] private UIWeaponUpgradePanel upgradePanel;
        [SerializeField] private GameObject upgradePanelRoot;
        
        [Header("Shard Info")]
        [SerializeField] private TMP_Text txtShard;
        [SerializeField] private Slider shardSlider;

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
                txtLevel.text = $"+{vm.Level}";

            BindStatLines(vm.EquipEffects);
            BindButtons(vm);

            if (upgradePanel != null)
                upgradePanel.Bind(vm.UpgradePanel, vm, currentHeroId, HandleUpgradePanelChanged);

            if (upgradePanelRoot != null)
                upgradePanelRoot.SetActive(false);
            else if (upgradePanel != null)
                upgradePanel.gameObject.SetActive(false);
            
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
                btnEquip.gameObject.SetActive(vm.ShowEquip);
                btnEquip.interactable = vm.CanEquip;
                btnEquip.onClick.RemoveAllListeners();
                btnEquip.onClick.AddListener(OnClickEquip);
            }

            if (btnAutoEquip != null)
            {
                btnAutoEquip.gameObject.SetActive(vm.ShowAutoEquip);
                btnAutoEquip.interactable = vm.CanAutoEquip;
                btnAutoEquip.onClick.RemoveAllListeners();
                btnAutoEquip.onClick.AddListener(OnClickAutoEquip);
            }

            if (btnOpenUpgrade != null)
            {
                btnOpenUpgrade.gameObject.SetActive(vm.ShowOpenUpgrade);
                btnOpenUpgrade.interactable = vm.CanOpenUpgrade;
                btnOpenUpgrade.onClick.RemoveAllListeners();
                btnOpenUpgrade.onClick.AddListener(OnClickOpenUpgrade);
            }

            if (btnFusion != null)
            {
                btnFusion.gameObject.SetActive(vm.ShowFusion);
                btnFusion.interactable = vm.CanFusion;
                btnFusion.onClick.RemoveAllListeners();
                btnFusion.onClick.AddListener(OnClickFusion);
            }

            if (btnFuseAll != null)
            {
                btnFuseAll.gameObject.SetActive(vm.ShowFuseAll);
                btnFuseAll.interactable = vm.CanFuseAll;
                btnFuseAll.onClick.RemoveAllListeners();
                btnFuseAll.onClick.AddListener(OnClickFuseAll);
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

        private void OnClickAutoEquip()
        {
            if (WeaponManager.Instance == null || currentVm == null)
                return;

            // Auto equip cho tất cả hero đang ra trận cùng class phù hợp là phase sau.
            // Hiện tại tối thiểu auto equip cho hero focus.
            WeaponManager.Instance.TryAutoEquip(currentHeroId, currentVm.HeroClass);
            RequestRefresh();
        }

        private void OnClickOpenUpgrade()
        {
            if (upgradePanelRoot != null)
                upgradePanelRoot.SetActive(true);
            else if (upgradePanel != null)
                upgradePanel.gameObject.SetActive(true);
        }

        private void OnClickFusion()
        {
            if (WeaponManager.Instance == null || currentVm == null || currentVm.IsExclusive)
                return;

            WeaponManager.Instance.TryFuseStandard(currentVm.WeaponId);
            RequestRefresh();
        }

        private void OnClickFuseAll()
        {
            // Phase sau: gọi service fuse all global
            Debug.Log("[WeaponDetailPanel] Fuse All not implemented yet.");
        }

        private void HandleUpgradePanelChanged()
        {
            RequestRefresh();
        }

        private void RequestRefresh()
        {
            onRequestRefresh?.Invoke();
        }
    }
}