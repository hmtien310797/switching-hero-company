using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class WeaponSummonView : BaseSummonPanelView
    {
        public override SummonCategory Category => SummonCategory.Weapon;

        [SerializeField] private TMP_Text placeholderText;

        protected override void OnShowPanel()
        {
            if (placeholderText != null)
                placeholderText.text = "Weapon Summon - Coming Soon";
        }
    }
}