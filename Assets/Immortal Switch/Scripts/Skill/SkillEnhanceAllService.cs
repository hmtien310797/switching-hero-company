using System.Collections.Generic;
using Immortal_Switch.Scripts.Skill.UI;

namespace Immortal_Switch.Scripts.Skill
{
    public class SkillEnhanceAllService
    {
        private readonly ISkillEnhanceRepository repository;
        private readonly SkillViewDataProvider dataProvider;

        public SkillEnhanceAllService(
            ISkillEnhanceRepository repository,
            SkillViewDataProvider dataProvider)
        {
            this.repository = repository;
            this.dataProvider = dataProvider;
        }

        public SkillEnhanceAllResult EnhanceAll()
        {
            var result = new SkillEnhanceAllResult();

            List<SkillEnhanceState> allStates = repository.GetAllSkillStates();
            if (allStates == null || allStates.Count == 0)
                return result;

            for (int i = 0; i < allStates.Count; i++)
            {
                SkillEnhanceState state = allStates[i];
                if (state == null)
                    continue;

                SkillDataSO skillData = dataProvider.GetSkillData(state.SkillId);
                if (skillData == null)
                    continue;

                result.ProcessedSkillCount++;

                int oldLevel = state.Level;
                int oldShard = state.ShardCount;
                bool wasUnlockedBefore = IsUnlocked(state);
                int spentForThisSkill = 0;

                if (!state.IsUnlocked && state.ShardCount > 0)
                    state.IsUnlocked = true;

                while (CanEnhance(state, skillData))
                {
                    int cost = skillData.GetRequiredShardForLevel(state.Level);
                    if (cost <= 0)
                        break;

                    state.ShardCount -= cost;
                    state.Level += 1;
                    spentForThisSkill += cost;
                }

                repository.SetSkillState(state.SkillId, state);

                bool changed = state.Level > oldLevel || IsUnlocked(state) != wasUnlockedBefore;
                if (!changed)
                    continue;

                if (state.Level > oldLevel)
                {
                    result.UpgradedSkillCount++;
                    result.TotalLevelGained += (state.Level - oldLevel);
                    result.TotalShardSpent += spentForThisSkill;
                }

                result.Entries.Add(new SkillEnhanceAllEntry
                {
                    SkillId = state.SkillId,
                    OldLevel = oldLevel,
                    NewLevel = state.Level,
                    OldShard = oldShard,
                    NewShard = state.ShardCount,
                    ShardSpent = spentForThisSkill,
                    WasUnlockedBefore = wasUnlockedBefore,
                    IsUnlockedAfter = IsUnlocked(state)
                });
            }

            repository.Save();
            return result;
        }

        public bool CanEnhanceAnySkill()
        {
            List<SkillEnhanceState> allStates = repository.GetAllSkillStates();
            if (allStates == null || allStates.Count == 0)
                return false;

            for (int i = 0; i < allStates.Count; i++)
            {
                SkillEnhanceState state = allStates[i];
                if (state == null)
                    continue;

                SkillDataSO skillData = dataProvider.GetSkillData(state.SkillId);
                if (skillData == null)
                    continue;

                if (CanEnhance(state, skillData))
                    return true;
            }

            return false;
        }

        public bool CanEnhance(int skillId)
        {
            SkillEnhanceState state = repository.GetSkillState(skillId);
            if (state == null)
                return false;

            SkillDataSO skillData = dataProvider.GetSkillData(skillId);
            if (skillData == null)
                return false;

            return CanEnhance(state, skillData);
        }

        private bool CanEnhance(SkillEnhanceState state, SkillDataSO skillData)
        {
            if (state == null || skillData == null)
                return false;

            if (!IsUnlocked(state))
                return false;

            if (skillData.IsMaxLevel(state.Level))
                return false;

            int cost = skillData.GetRequiredShardForLevel(state.Level);
            return cost > 0 && state.ShardCount >= cost;
        }

        private bool IsUnlocked(SkillEnhanceState state)
        {
            if (state == null)
                return false;

            return state.IsUnlocked || state.ShardCount > 0;
        }
    }
}