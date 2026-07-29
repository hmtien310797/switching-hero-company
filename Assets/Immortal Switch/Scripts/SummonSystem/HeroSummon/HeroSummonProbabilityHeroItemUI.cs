using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.HeroUIView;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.HeroSummon
{
    public class HeroSummonProbabilityHeroItemUI : MonoBehaviour
    {
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text heroNameText;
        [SerializeField] private TMP_Text probabilityText;

        [Header("Optional")]
        [SerializeField] private Image classIcon;
        [SerializeField] private Image elementIcon;
        
        [SerializeField] private HeroUIIconConfigSO iconConfig;

        public void Bind(HeroSummonProbabilityHeroData data, ElementIconEntry element)
        {
            if (data == null || data.Hero == null)
                return;

            if (portraitImage != null)
                portraitImage.sprite = HeroImageService.GetHeroIcon(data.Hero);

            if (heroNameText != null)
                heroNameText.text = data.Hero.Name;

            if (probabilityText != null)
                probabilityText.text = $"{data.ProbabilityPercent:0.####}%";

            elementIcon.sprite = element.Icon;
            classIcon.sprite = HeroImageService.GetHeroClassIcon(data.Hero);

            // if (tierConfig != null)
            // {
            //     var visual = tierConfig.Get(data.Hero.SummonRarity);
            //
            //     if (visual == null) return;
            // }
        }
    }
}