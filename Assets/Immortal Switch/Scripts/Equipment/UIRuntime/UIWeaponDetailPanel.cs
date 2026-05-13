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
        
        [Header("Shard Info")]
        [SerializeField] private TMP_Text txtShard;
        [SerializeField] private Image shardSlider;
        
        [Header("Tier Visual")]
        [SerializeField] private Image imgTierLabel;
        [SerializeField] private Image imgTierBackground;
        [SerializeField] private WeaponTierVisualConfigSO tierVisualConfig;

        [Header("Star Display")]
        [SerializeField] private UIWeaponStarDisplay starDisplay;
        
        [Header("Fuse All Result Popup")]
        [SerializeField] private UIWeaponFuseAllResultPopup fuseAllResultPopup;
        
        [Header("Fusion Popup")]
        [SerializeField] private UIWeaponFusionPopup fusionPopup;
        [SerializeField] private WeaponViewDataProvider weaponViewDataProvider;

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

            if (txtLevel != null)
                txtLevel.text = $"+{vm.Level}";

            BindStatLines(vm.EquipEffects);
            BindButtons(vm);
            BindTierVisual(vm.Tier);
            starDisplay.BindStandard(vm.Star);

            if (upgradePanel != null)
            {
                upgradePanel.gameObject.SetActive(false);
                upgradePanel.Bind(vm.UpgradePanel, vm, currentHeroId, HandleUpgradePanelChanged);
            }
            
            if (txtShard != null)
            {
                txtShard.text = vm.MaxShard > 0
                    ? $"{vm.CurrentShard}/{vm.MaxShard}"
                    : vm.CurrentShard.ToString();
            }

            if (shardSlider != null)
            {
                shardSlider.fillAmount = vm.ShardProgressNormalized;
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
        
        private void BindTierVisual(WeaponTier tier)
        {
            if (tierVisualConfig == null)
                return;

            var entry = tierVisualConfig.Get(tier);
            if (entry == null)
                return;

            if (imgTierLabel != null)
                imgTierLabel.sprite = entry.TierLabelSprite;

            if (imgTierBackground != null)
                imgTierBackground.sprite = entry.TierBackgroundSprite;
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
            if (WeaponManager.Instance == null)
                return;

            if (Battle.PvEBattleController.Instance == null)
                return;

            var activeHeroes = Battle.PvEBattleController.Instance.GetActiveHeroControllers();
            WeaponManager.Instance.TryAutoEquipForHeroes(activeHeroes);

            RequestRefresh();
        }

        private void OnClickOpenUpgrade()
        {
            upgradePanel.gameObject.SetActive(true);
        }

        private void OnClickFusion()
        {
            if (currentVm == null || currentVm.IsExclusive || fusionPopup == null || weaponViewDataProvider == null)
                return;

            var vm = weaponViewDataProvider.BuildFusionPopup(currentVm.WeaponId);
            if (vm == null)
                return;

            fusionPopup.Show(vm, RequestRefresh);
        }

        private void OnClickFuseAll()
        {
            if (WeaponManager.Instance == null)
                return;

            var result = WeaponManager.Instance.TryFuseAllStandardWeapons();

            if (result != null && result.HasAnyReward && fuseAllResultPopup != null)
                fuseAllResultPopup.Show(result);

            RequestRefresh();
        }
        
        private WeaponFuseAllResult BuildMockFuseAllResultForPreview()
        {
            var result = new WeaponFuseAllResult();

            if (WeaponManager.Instance == null || WeaponManager.Instance.Database == null)
                return result;

            var database = WeaponManager.Instance.Database;

            // lấy vài món sample từ database để test popup
            var std1 = database.GetStandard(1001);
            var std2 = database.GetStandard(1006);
            var std3 = database.GetStandard(1011);

            if (std1 != null)
            {
                result.Rewards.Add(new WeaponFuseAllRewardEntry
                {
                    WeaponId = std1.WeaponId,
                    WeaponName = std1.WeaponName,
                    HeroClass = std1.WeaponClass,
                    Tier = std1.Tier,
                    Star = std1.Star,
                    IsExclusive = false,
                    Amount = 1,
                    Icon = std1.Icon
                });
            }

            if (std2 != null)
            {
                result.Rewards.Add(new WeaponFuseAllRewardEntry
                {
                    WeaponId = std2.WeaponId,
                    WeaponName = std2.WeaponName,
                    HeroClass = std2.WeaponClass,
                    Tier = std2.Tier,
                    Star = std2.Star,
                    IsExclusive = false,
                    Amount = 2,
                    Icon = std2.Icon
                });
            }

            if (std3 != null)
            {
                result.Rewards.Add(new WeaponFuseAllRewardEntry
                {
                    WeaponId = std3.WeaponId,
                    WeaponName = std3.WeaponName,
                    HeroClass = std3.WeaponClass,
                    Tier = std3.Tier,
                    Star = std3.Star,
                    IsExclusive = false,
                    Amount = 1,
                    Icon = std3.Icon
                });
            }

            return result;
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