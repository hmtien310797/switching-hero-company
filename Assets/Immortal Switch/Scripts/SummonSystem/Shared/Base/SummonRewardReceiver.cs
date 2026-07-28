using System;
using System.Collections.Generic;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Helper;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using Immortal_Switch.Scripts.SummonSystem.WeaponSummon;
using Immortal_Switch.Scripts.UI;
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
                    if (summonLevel == DatabaseManager.Instance.HeroSummonConfig.LevelRewards.Count)
                    {
                        await PeekAsync(nameof(SummonCategory.Hero).ToLower());
                    }
                    else
                    {
                        await PersistClaimOnServerAsync(summonLevel, summonCategory);
                    }
                    break;
                case SummonCategory.Skill:
                    if (summonLevel == DatabaseManager.Instance.SkillSummonConfig.LevelRewards.Count)
                    {
                        await PeekAsync(nameof(SummonCategory.Skill).ToLower());
                    }
                    else
                    {
                        await PersistClaimOnServerAsync(summonLevel, summonCategory);
                    }
                    break;
                case SummonCategory.Weapon:
                    if (summonLevel == DatabaseManager.Instance.WeaponSummonConfig.LevelRewards.Count)
                    {
                        await PeekAsync(nameof(SummonCategory.Weapon).ToLower());
                    }
                    else
                    {
                        await PersistClaimOnServerAsync(summonLevel, summonCategory);
                    }
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

                // Server có thể sweep-claim nhiều mốc bị bỏ sót trong 1 lần gọi (không chỉ
                // summonLevel được request) — đánh dấu đúng những mốc đó cục bộ thay vì chỉ mốc
                // đã truyền vào, nếu không achievement list sẽ hiện sai trạng thái "chưa nhận".
                if (result.Rewards != null)
                {
                    var claimedLevels = new HashSet<int>();
                    foreach (var r in result.Rewards)
                        if (r.SummonLevel > 0) claimedLevels.Add(r.SummonLevel);

                    if (claimedLevels.Count == 0)
                        claimedLevels.Add(summonLevel);

                    switch (category)
                    {
                        case SummonCategory.Hero:
                            HeroSummonManager.Instance?.MarkRewardLevelsClaimed(claimedLevels);
                            break;
                        case SummonCategory.Skill:
                            SkillSummonManager.Instance?.MarkRewardLevelsClaimed(claimedLevels);
                            break;
                        case SummonCategory.Weapon:
                            WeaponSummonManager.Instance?.MarkRewardLevelsClaimed(claimedLevels);
                            break;
                    }
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

                if (result.Rewards != null)
                {
                    var itemRewards = new List<ItemData>();
                    foreach (var r in result.Rewards)
                    {
                        if (r.ItemId > 0 && r.Amount > 0)
                            itemRewards.Add(new ItemData(r.ItemId, r.Amount));
                    }

                    if (itemRewards.Count > 0)
                        PopupRewardService.Show(itemRewards);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ClaimReward] Server call failed level={summonLevel} category={category}: {ex.Message}");
            }
        }
        
        
        /// <summary>Xem trước 1 kết quả roll ngẫu nhiên theo tỉ lệ hiện tại — không tốn tiền,
        /// không ghi nhận vào summon_state hay kho đồ (xem NakamaClient.SummonPeekAsync).</summary>
        private async UniTask PeekAsync(string type)
        {
            if (!NakamaClient.Instance.IsLoggedIn)
            {
                Debug.LogWarning("[HeroSummon] No active session — not logged in.");
                return;
            }

            try
            {
                var response = await NakamaClient.Instance.SummonPeekAsync(type);

                if (!response.Success)
                {
                    Debug.LogWarning($"[HeroSummon] summon/peek failed: {response.Error}");
                    return;
                }

                switch (type)
                {
                    case "hero":
                        PopupRewardService.ShowHeroItemReward(new []{new HeroItemData(response.HeroId, 1)});
                        break;
                    case "skill":
                        if (Enum.TryParse<SkillSummonGrade>(response.Grade, out var grade))
                        {
                            PopupRewardService.ShowSkillItemReward(new []{new SkillItemData(response.SkillId, 1, grade)});
                        }
                        break;
                    case "weapon":
                        PopupRewardService.ShowWeaponItemReward(new []{new WeaponItemData(response.WeaponId, 1, response.Grade, response.Star)});
                        break;
                        
                }
            }
            catch (Nakama.ApiResponseException ex)
            {
                Debug.LogError($"[HeroSummon] summon/peek error {ex.StatusCode}: {ex.Message}");
            }
        }
    }
}