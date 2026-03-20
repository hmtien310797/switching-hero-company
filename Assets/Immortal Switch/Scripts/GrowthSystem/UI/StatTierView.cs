using System;
using Immortal_Switch.Scripts.StatSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    public class StatTierView : MonoBehaviour
    {
        [SerializeField] private Image statIcon;
        [SerializeField] private TMP_Text statName;
        [SerializeField] private Image statProgress;
        [SerializeField] private TMP_Text statCurrentStackValue;
        [SerializeField] private Button levelUpButton;
        [SerializeField] private TMP_Text statValuePay;
        [SerializeField] private Image maxImage;

        private StatType currentStat;
        private Action<StatType> onClickUpgrade;

        public void Initialize(StatTierViewData viewData, Action<StatType> onUpgradeClicked)
        {
            currentStat = viewData.Stat;
            onClickUpgrade = onUpgradeClicked;

            if (statIcon != null) statIcon.sprite = viewData.Icon;
            if (statName != null) statName.text = viewData.Name;
            if (statProgress != null) statProgress.fillAmount = viewData.StatProgressPercent;
            if (statCurrentStackValue != null) statCurrentStackValue.text = $"{viewData.StatCurrentStack}/{viewData.StatMaxStack}";
            if (statValuePay != null) statValuePay.text = viewData.ValuePay;
            if (maxImage != null) maxImage.enabled = viewData.IsMax;

            if (levelUpButton != null)
            {
                levelUpButton.onClick.RemoveListener(OnClickLevelUp);
                levelUpButton.interactable = viewData.CanUpgrade;
                levelUpButton.onClick.AddListener(OnClickLevelUp);
            }
        }

        private void OnClickLevelUp()
        {
            onClickUpgrade?.Invoke(currentStat);
        }

        private void OnDestroy()
        {
            if (levelUpButton != null)
            {
                levelUpButton.onClick.RemoveListener(OnClickLevelUp);
            }
        }
    }

    public struct StatTierViewData
    {
        public StatType Stat;
        public Sprite Icon;
        public string Name;
        public float StatProgressPercent;
        public int StatCurrentStack;
        public int StatMaxStack;
        public string ValuePay;
        public bool IsMax;
        public bool CanUpgrade;
    }
}