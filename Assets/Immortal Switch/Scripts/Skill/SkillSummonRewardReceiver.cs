using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.SkillSummon;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    public interface ISkillSummonRewardReceiver
    {
        void GrantReward(SkillSummonRewardItem rewardItem);
    }

    public class SkillSummonRewardReceiver : MonoBehaviour, ISkillSummonRewardReceiver
    {
        public void GrantReward(SkillSummonRewardItem rewardItem)
        {
            if (rewardItem == null || SkillSummonManager.Instance == null || SkillSummonManager.Instance.Service == null)
                return;

            switch (rewardItem.RewardType)
            {
                case SkillSummonRewardType.Currency:
                    CurrencyManager.Instance.Add(rewardItem.CurrencyType, rewardItem.Amount);
                    break;

                case SkillSummonRewardType.RandomSkill:
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
    }
}