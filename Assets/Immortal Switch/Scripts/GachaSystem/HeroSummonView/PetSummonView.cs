using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class PetSummonView : BaseSummonPanelView
    {
        public override SummonCategory Category => SummonCategory.Pet;

        [SerializeField] private TMP_Text placeholderText;

        protected override void OnShowPanel()
        {
            if (placeholderText != null)
                placeholderText.text = "Pet Summon - Coming Soon";
        }
    }
}