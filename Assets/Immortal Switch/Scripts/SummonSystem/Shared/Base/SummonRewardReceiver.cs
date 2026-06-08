using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using Immortal_Switch.Scripts.SummonSystem.WeaponSummon;
using UnityEngine;

namespace Immortal_Switch.Scripts.SummonSystem.Shared.Base
{
    public class SummonRewardReceiver : MonoBehaviour, ISummonRewardReceiver
    {
        public void GrantReward(SummonRewardItem rewardItem)
        {
            if (rewardItem == null)
                return;

            switch (rewardItem.RewardType)
            {
                case SummonRewardType.Currency:
                    GrantCurrency(rewardItem);
                    break;

                case SummonRewardType.RandomHero:
                    GrantRandomHero(rewardItem);
                    break;
                
                case SummonRewardType.RandomSkill:
                    for (int i = 0; i < rewardItem.Amount; i++)
                    {
                        var skill = SkillSummonManager.Instance.Service.GetRandomSkillByGrade(rewardItem.RandomSkillGrade);
                        if (skill == null)
                        {
                            Debug.LogWarning($"No skill found for grade {rewardItem.RandomSkillGrade}");
                            continue;
                        }

                        SkillSummonManager.Instance.Service.ProgressionService.AcquireOrAddDuplicate(skill, 1);
                    }
                    break;
            }
        }

        public SummonRewardPreviewData GetRewardPreviewData(SummonCategory summonCategory)
        {
            switch (summonCategory)
            {
                case SummonCategory.Hero:
                    return HeroSummonManager.Instance.Service.GetRewardPreviewData();
                case SummonCategory.Skill:
                    return SkillSummonManager.Instance.Service.GetRewardPreviewData();
                case SummonCategory.Weapon:
                    return WeaponSummonManager.Instance.Service.GetRewardPreviewData();
                default:
                    return null;
            }
        }

        public bool ClaimReward(int summonLevel, ISummonRewardReceiver rewardReceiver, SummonCategory summonCategory)
        {
            switch (summonCategory)
            {
                case SummonCategory.Hero:
                    return HeroSummonManager.Instance.ClaimReward(summonLevel, rewardReceiver);
                case SummonCategory.Skill:
                    return SkillSummonManager.Instance.ClaimReward(summonLevel, rewardReceiver);
                case SummonCategory.Weapon:
                    return WeaponSummonManager.Instance.ClaimReward(summonLevel, rewardReceiver);
                default:
                    return false;
            }
        }


        private void GrantCurrency(SummonRewardItem rewardItem)
        {
            CurrencyManager.Instance.AddLocalDemo(rewardItem.CurrencyType, rewardItem.Amount);
        }

        private void GrantRandomHero(SummonRewardItem rewardItem)
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