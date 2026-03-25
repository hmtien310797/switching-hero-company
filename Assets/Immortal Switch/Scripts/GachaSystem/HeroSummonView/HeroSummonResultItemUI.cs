using Immortal_Switch.Hero;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class HeroSummonResultItemUI : MonoBehaviour
    {
        [SerializeField] private Image heroPortrait;
        [SerializeField] private TMP_Text heroNameText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private GameObject newHeroTag;
        [SerializeField] private GameObject duplicateTag;

        public void Bind(HeroSummonResultEntry entry)
        {
            var hero = entry.HeroAsset as HeroDataSO;

            if (heroPortrait != null && hero != null)
                heroPortrait.sprite = hero.PortraitIcon;

            if (heroNameText != null)
                heroNameText.text = entry.HeroName;

            if (newHeroTag != null)
                newHeroTag.SetActive(entry.IsNewHero);

            if (duplicateTag != null)
                duplicateTag.SetActive(!entry.IsNewHero);

            if (stateText != null)
            {
                if (entry.IsNewHero)
                    stateText.text = "New Hero";
                else
                    stateText.text = $"+{entry.ShardGained} Shard";
            }
        }
    }
}