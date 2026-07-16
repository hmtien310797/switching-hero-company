using System.Collections.Generic;
using Common;
using Cysharp.Threading.Tasks;
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

            // switch (rewardItem.RewardType)
            // {
            //     case SummonRewardType.Currency:
            //         GrantCurrency(rewardItem);
            //         break;
            //
            //     case SummonRewardType.RandomHero:
            //         GrantRandomHero(rewardItem);
            //         break;
            //     
            //     case SummonRewardType.RandomSkill:
            //         // for (int i = 0; i < rewardItem.Amount; i++)
            //         // {
            //         //     var skill = SkillSummonManager.Instance.Service.GetRandomSkillByGrade(rewardItem.RandomSkillGrade);
            //         //     if (skill == null)
            //         //     {
            //         //         Debug.LogWarning($"No skill found for grade {rewardItem.RandomSkillGrade}");
            //         //         continue;
            //         //     }
            //         //
            //         //     SkillSummonManager.Instance.Service.ProgressionService.AcquireOrAddDuplicate(skill, 1);
            //         // }
            //         break;
            // }
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

        public async UniTask ClaimReward(int summonLevel, ISummonRewardReceiver rewardReceiver, SummonCategory summonCategory)
        {
            switch (summonCategory)
            {
                case SummonCategory.Hero:
                    await PersistClaimOnServerAsync(summonLevel, summonCategory);
                    break;
                case SummonCategory.Skill:
                    await PersistClaimOnServerAsync(summonLevel, summonCategory);
                    break;
                case SummonCategory.Weapon:
                    await PersistClaimOnServerAsync(summonLevel, summonCategory);
                    break;
            }
        }

        private async UniTask PersistClaimOnServerAsync(int summonLevel, SummonCategory category)
        {
            try
            {
                ClaimRewardResponse result = category switch
                {
                    SummonCategory.Hero   => await NakamaClient.Instance.SummonHeroClaimRewardAsync(summonLevel),
                    SummonCategory.Skill  => await NakamaClient.Instance.SummonSkillClaimRewardAsync(summonLevel),
                    SummonCategory.Weapon => await NakamaClient.Instance.SummonWeaponClaimRewardAsync(summonLevel),
                    _ => null
                };

                if (result == null || !result.Success)
                {
                    Debug.LogWarning($"[ClaimReward] Server rejected claim level={summonLevel} category={category}: {result?.Error}");
                    return;
                }

                if (result.CurrencyBalances != null)
                {
                    CurrencyManager.Instance.Set(CurrencyType.summon_ticket_hero,   result.CurrencyBalances.HeroTicket);
                    CurrencyManager.Instance.Set(CurrencyType.summon_ticket_skill,  result.CurrencyBalances.SkillTicket);
                    CurrencyManager.Instance.Set(CurrencyType.summon_ticket_weapon, result.CurrencyBalances.WeaponTicket);
                    CurrencyManager.Instance.Set(CurrencyType.diamond,      result.CurrencyBalances.Diamond);
                }

                if (category == SummonCategory.Skill && result.Rewards != null)
                {
                    var entries = new List<SummonEntry>();
                    foreach (var r in result.Rewards)
                    {
                        if (r.SkillId > 0 && !string.IsNullOrEmpty(r.SkillUid))
                            entries.Add(new SummonEntry { SkillId = r.SkillId, SkillUid = r.SkillUid, IsNew = r.IsNew, ShardGained = r.ShardGained });
                    }
                    if (entries.Count > 0)
                        UserDataCache.Instance?.ApplySkillSummonEntries(entries.ToArray());
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ClaimReward] Server call failed level={summonLevel} category={category}: {ex.Message}");
            }
        }


        private void GrantCurrency(SummonRewardItem rewardItem)
        {
            // đồng bộ trên server
            // CurrencyLedgerService.Instance.AddOrMergeIncome(
            //     rewardItem.ItemId,
            //     rewardItem.Amount,
            //     CurrencyTransactionReason.Summon
            // );
        }

        private void GrantRandomHero(SummonRewardItem rewardItem)
        {
            // if (HeroSummonManager.Instance == null || HeroSummonManager.Instance.Service == null)
            //     return;
            //
            // for (int i = 0; i < rewardItem.Amount; i++)
            // {
            //     var hero = HeroSummonManager.Instance.Service.GetRandomHeroByRarity(rewardItem.HeroRarity);
            //     if (hero == null)
            //     {
            //         Debug.LogWarning($"No hero found for rarity {rewardItem.HeroRarity}");
            //         continue;
            //     }
            //
            //     bool alreadyOwned = HeroProgressionManager.Instance.Service.HasHero(hero.Id);
            //     if (alreadyOwned)
            //         HeroProgressionManager.Instance.AddShardToHero(hero, 1, true);
            //     else
            //         HeroProgressionManager.Instance.AcquireHeroIfNeeded(hero);
            // }
        }
    }
}