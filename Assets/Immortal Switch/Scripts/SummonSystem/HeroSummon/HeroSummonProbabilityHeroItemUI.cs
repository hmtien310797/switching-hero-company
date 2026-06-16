using Immortal_Switch.Scripts.HeroUIView;
using Immortal_Switch.Scripts.UI;
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
        [SerializeField] private HeroSummonRarityVisualConfigSO tierConfig;

        [SerializeField] private UIGradient gradient;

        public void Bind(HeroSummonProbabilityHeroData data, SpriteAtlas heroSpriteAtlas)
        {
            if (data == null || data.Hero == null)
                return;

            if (portraitImage != null)
                portraitImage.sprite = heroSpriteAtlas.GetSprite(data.Hero.HeroIconKey);

            if (heroNameText != null)
                heroNameText.text = data.Hero.Name;

            if (probabilityText != null)
                probabilityText.text = $"{data.ProbabilityPercent:0.####}%";

            if (tierConfig != null)
            {
                var visual = tierConfig.Get(data.Hero.SummonRarity);

                if (visual == null) return;
                if (gradient == null) return;
                gradient.enabled = true;
                gradient.Refresh(visual.TopColor, visual.BottomColor);
            }
        }
    }
}