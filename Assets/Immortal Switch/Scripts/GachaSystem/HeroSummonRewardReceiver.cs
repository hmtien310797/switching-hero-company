using Immortal_Switch.Hero;
using Immortal_Switch.Scripts.Currency;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem
{
    public class HeroSummonRewardReceiver : MonoBehaviour, IHeroSummonRewardReceiver
    {
        public void GrantReward(HeroSummonRewardItem rewardItem)
        {
            if (rewardItem == null)
                return;

            switch (rewardItem.RewardType)
            {
                case HeroSummonRewardType.Currency:
                    GrantCurrency(rewardItem);
                    break;

                case HeroSummonRewardType.RandomHero:
                    GrantRandomHero(rewardItem);
                    break;
            }
        }

        private void GrantCurrency(HeroSummonRewardItem rewardItem)
        {
            CurrencyManager.Instance.Add(rewardItem.CurrencyType, rewardItem.Amount);
        }

        private void GrantRandomHero(HeroSummonRewardItem rewardItem)
        {
            if (HeroSummonManager.Instance == null || HeroSummonManager.Instance.Service == null)
                return;

            for (int i = 0; i < rewardItem.Amount; i++)
            {
                var hero = HeroSummonManager.Instance.Service.GetRandomHeroByRarity(rewardItem.HeroRarity);
                if (hero == null)
                {
                    Debug.LogWarning($"No hero found for rarity {rewardItem.HeroRarity}");
                    continue;
                }

                bool alreadyOwned = HeroProgressionManager.Instance.Service.HasHero(hero.Id);
                if (alreadyOwned)
                    HeroProgressionManager.Instance.AddShardToHero(hero, 1, true);
                else
                    HeroProgressionManager.Instance.AcquireHeroIfNeeded(hero);
            }
        }
    }
}