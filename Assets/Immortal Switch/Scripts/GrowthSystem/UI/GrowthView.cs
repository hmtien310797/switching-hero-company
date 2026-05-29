using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.PowerUpSystem;
 using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.UI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    public class GrowthView : AnimatedUIView
    {
        [SerializeField] private GrowthUpgradePanelView panelView;
        [SerializeField] private GrowthStatUIViewDatabaseSO statUiDatabase;

        [Header("Tier Popup")]
        [PreviewField]
        [SerializeField] private Sprite[] tierIcons; 

        [Header("UI State")]
        [SerializeField] private int selectedUpgradeAmount = 1;

        private GrowthManager growthManager;
        private GrowthUpgradePanelBinder binder;
        private GrowthTierUpgradePopupBinder popupBinder;

        private void Awake()
        {
            growthManager = GrowthManager.Instance;

            binder = new GrowthUpgradePanelBinder(
                growthManager.Service,
                statUiDatabase
            );

            popupBinder = new GrowthTierUpgradePopupBinder(
                growthManager.Service,
                statUiDatabase
            );
        }

        private void OnEnable()
        {
            growthManager.OnGrowthChanged += RefreshUI;
            growthManager.OnTierReadyToUpgradePopup += HandleTierReadyPopup;
            RefreshUI();
            growthManager.CheckAndNotifyTierReady();
        }

        private void OnDisable()
        {
            growthManager.OnGrowthChanged -= RefreshUI;
            growthManager.OnTierReadyToUpgradePopup -= HandleTierReadyPopup;
        }

        public void RefreshUI()
        {
            int currentGold = CurrencyManager.Instance.Get(CurrencyType.Gold);
            var panelData = binder.Build(
                currentGold,
                selectedUpgradeAmount
            );

            panelView.Bind(
                panelData,
                currentGold,
                selectedUpgradeAmount,
                OnClickUpgradeStat,
                OnChangeUpgradeAmount
            );
        }

        private void OnChangeUpgradeAmount(int amount)
        {
            selectedUpgradeAmount = amount;
            RefreshUI();
        }

        private void OnClickUpgradeStat(StatType stat)
        {
            growthManager.TryUpgrade(stat, selectedUpgradeAmount);
        }

        private void HandleTierReadyPopup(int currentTier, int newTier, bool isActive = true)
        {
            HandleTierReadyPopupAsync(currentTier, newTier, isActive).Forget();
        }

        private async UniTask HandleTierReadyPopupAsync(int currentTier, int nextTier, bool isActive = true)
        {
            if (!isActive)
                return;
            
            var tierUpgradePopupView = await UIManager.Instance.OpenPopupAsync<GrowthTierUpgradePopupView>(withBackdrop: false);

            var currentIcon = GetIconForTier(currentTier);
            var nextIcon = GetIconForTier(nextTier);

            var popupData = popupBinder.Build(
                currentTier,
                nextTier,
                currentIcon,
                nextIcon
            );

            tierUpgradePopupView.Show(popupData, () =>
            {
                growthManager.UnlockTier(nextTier);
                tierUpgradePopupView.Hide();
                RefreshUI();
            });
        }

        private Sprite GetIconForTier(int tier)
        {
            if (tierIcons == null || tierIcons.Length == 0)
                return null;

            int iconIndex = Mathf.Max(0, (tier - 1) / 10);
            iconIndex = Mathf.Clamp(iconIndex, 0, tierIcons.Length - 1);
            return tierIcons[iconIndex];
        }
    }
}