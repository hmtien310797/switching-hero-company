using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.HeroUIView;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.HeroSummon
{
    public class HeroSummonProbabilitySectionUI : MonoBehaviour
    {
        [Header("Configs")]
        [SerializeField] private HeroSummonRarityVisualConfigSO heroSummonRarityVisualConfig;
        [SerializeField] private HeroUIIconConfigSO heroUIConfig;

        [SerializeField] private Image imgRarity;
        [SerializeField] private TMP_Text totalRateText;
        [SerializeField] private Transform itemRoot;
        [SerializeField] private HeroSummonProbabilityHeroItemUI itemPrefab;

        private SimpleUIPool<HeroSummonProbabilityHeroItemUI> itemPool;

        private void Awake()
        {
            itemPool = new SimpleUIPool<HeroSummonProbabilityHeroItemUI>(itemPrefab, itemRoot);
        }

        public void Bind(HeroSummonProbabilitySectionData data, SpriteAtlas heroSpriteAtlas)
        {
            var spr = heroSummonRarityVisualConfig.GetIcon(data.Rarity);

            if (spr != null)
            {
                imgRarity.sprite = spr;
            }

            if (totalRateText != null)
                totalRateText.text = data != null ? $"{data.TotalRatePercent:0.####}%" : "";

            if (data == null || data.Heroes == null)
            {
                itemPool.ReleaseFrom(0);
                return;
            }

            for (int i = 0; i < data.Heroes.Count; i++)
            {
                var hero = data.Heroes[i];
                var item = itemPool.Get(i);
                var element = heroUIConfig.GetElement(hero.Hero.Element);
                var heroClass = heroUIConfig.GetHeroClass(hero.Hero.HeroClass);

                if (element == null ||
                    heroClass == null)
                {
                    Debug.LogError($"Element {i} of hero {hero.Hero.Name} not found");
                    continue;
                }
                item.Bind(hero, heroSpriteAtlas, element, heroClass);
            }

            itemPool.ReleaseFrom(data.Heroes.Count);
        }
    }
}