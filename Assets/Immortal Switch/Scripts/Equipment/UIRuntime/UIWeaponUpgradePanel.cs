using Immortal_Switch.Scripts.Equipment.UI;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    public class UIWeaponUpgradePanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text txtLevelRange;
        [SerializeField] private TMP_Text txtNextLevelCost;
        [SerializeField] private TMP_Text txtLevelUpAllCost;
        [SerializeField] private TMP_Text txtBreakCost;
        [SerializeField] private TMP_Text txtBreakRate;
        [SerializeField] private TMP_Text txtNextBreakLevel;

        public void Bind(WeaponUpgradePanelViewModel vm)
        {
            if (txtLevelRange != null)
                txtLevelRange.text = $"{vm.CurrentLevel}/{vm.CurrentMaxLevel}";

            if (txtNextLevelCost != null)
                txtNextLevelCost.text = vm.NextLevelCost.ToString();

            if (txtLevelUpAllCost != null)
                txtLevelUpAllCost.text = vm.LevelUpAllCost.ToString();

            if (txtBreakCost != null)
                txtBreakCost.text = vm.BreakThroughCost.ToString();

            if (txtBreakRate != null)
                txtBreakRate.text = $"{vm.LimitBreakSuccessRate * 100f:0.##}%";

            if (txtNextBreakLevel != null)
                txtNextBreakLevel.text = vm.NextBreakRequiredLevel.ToString();
        }
    }
}