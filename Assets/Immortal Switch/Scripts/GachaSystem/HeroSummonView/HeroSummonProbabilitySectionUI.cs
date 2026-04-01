using Immortal_Switch.Scripts.Core;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class HeroSummonProbabilitySectionUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text rarityText;
        [SerializeField] private TMP_Text totalRateText;
        [SerializeField] private Transform itemRoot;
        [SerializeField] private HeroSummonProbabilityHeroItemUI itemPrefab;

        private SimpleUIPool<HeroSummonProbabilityHeroItemUI> itemPool;

        private void Awake()
        {
            itemPool = new SimpleUIPool<HeroSummonProbabilityHeroItemUI>(itemPrefab, itemRoot);
        }

        public void Bind(HeroSummonProbabilitySectionData data)
        {
            if (rarityText != null)
                rarityText.text = data?.RarityLabel ?? "";

            if (totalRateText != null)
                totalRateText.text = data != null ? $"{data.TotalRatePercent:0.####}%" : "";

            if (data == null || data.Heroes == null)
            {
                itemPool.ReleaseFrom(0);
                return;
            }

            for (int i = 0; i < data.Heroes.Count; i++)
            {
                var item = itemPool.Get(i);
                item.Bind(data.Heroes[i]);
            }

            itemPool.ReleaseFrom(data.Heroes.Count);
        }
    }
}