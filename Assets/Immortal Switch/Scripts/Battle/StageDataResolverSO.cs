using System;
using System.Collections.Generic;
using Battle;
using Immortal_Switch.Scripts.Boss;
using UnityEngine;

namespace Immortal_Switch.Scripts.Level.Stage
{
    [CreateAssetMenu(fileName = "StageDataResolver", menuName = "ScriptableObjects/Stage/StageDataResolver")]
    public class StageDataResolverSO : ScriptableObject
    {
        [SerializeField] private ChapterConfigSO chapterConfig;
        [SerializeField] private EnemyPatternRuleSO enemyPatternRuleConfig;
        [SerializeField] private BossPatternRuleSO bossPatternRuleConfig;
        [SerializeField] private RewardRuleSO rewardRuleConfig;

        public int GetChapterIndexByStage(int globalStage)
        {
            if (chapterConfig == null || chapterConfig.Chapters == null)
                return 0;

            globalStage = Mathf.Max(1, globalStage);

            int accumulatedStage = 0;

            for (int i = 0; i < chapterConfig.Chapters.Length; i++)
            {
                ChapterConfig chapter = chapterConfig.Chapters[i];
                if (chapter == null || chapter.StageCount <= 0)
                    continue;

                int chapterEndStage = accumulatedStage + chapter.StageCount;

                if (globalStage <= chapterEndStage)
                    return i;

                accumulatedStage = chapterEndStage;
            }

            return Mathf.Max(0, chapterConfig.Chapters.Length - 1);
        }

        public StageRuntimeData Resolve(int globalStage)
        {
            globalStage = Mathf.Max(1, globalStage);

            if (!TryResolveChapter(globalStage, out ChapterConfig chapter, out int chapterIndex, out int localStage))
            {
                Debug.LogError($"[StageResolver] Cannot resolve chapter. globalStage={globalStage}");
                return null;
            }

            EnemyPatternRule enemyRule = FindEnemyRule(chapter.EnemyPatternRuleId);
            if (enemyRule == null)
            {
                Debug.LogError($"[StageResolver] Cannot find enemy rule: {chapter.EnemyPatternRuleId}");
                return null;
            }

            if (enemyRule.RequiredElement != chapter.ChapterElement)
            {
                Debug.LogError(
                    $"[StageResolver] Chapter element mismatch enemy rule. " +
                    $"chapter={chapter.ChapterName}, chapterElement={chapter.ChapterElement}, " +
                    $"rule={enemyRule.RuleId}, ruleElement={enemyRule.RequiredElement}"
                );
                return null;
            }

            EnemyPatternData enemyPattern = ResolveEnemyPattern(enemyRule, localStage);
            if (enemyPattern == null)
            {
                Debug.LogError(
                    $"[StageResolver] Cannot resolve enemy pattern. rule={enemyRule.RuleId}, localStage={localStage}");
                return null;
            }

            BossPatternRule bossRule = FindBossRule(chapter.BossPatternRuleId);
            if (bossRule == null)
            {
                Debug.LogError($"[StageResolver] Cannot find boss rule: {chapter.BossPatternRuleId}");
                return null;
            }

            if (bossRule.RequiredElement != chapter.ChapterElement)
            {
                Debug.LogError(
                    $"[StageResolver] Chapter element mismatch boss rule. " +
                    $"chapter={chapter.ChapterName}, chapterElement={chapter.ChapterElement}, " +
                    $"rule={bossRule.RuleId}, ruleElement={bossRule.RequiredElement}"
                );
                return null;
            }

            int bossId = ResolveBossId(bossRule, localStage);

            StageRuntimeData runtimeData = new StageRuntimeData
            {
                GlobalStage = globalStage,
                ChapterIndex = chapterIndex,
                ChapterId = chapter.ChapterId,
                ChapterName = chapter.ChapterName,
                LocalStage = localStage,
                ChapterElement = chapter.ChapterElement,

                EnemyPatternRuleId = enemyRule.RuleId,
                EnemyPatternId = enemyPattern.PatternId,
                EnemyIds = enemyPattern.EnemyIds,
                EnemyRates = NormalizeRates(enemyPattern.Rates),

                BossPatternRuleId = bossRule.RuleId,
                BossId = bossId,

                AfkRewardMultiplier = Mathf.Max(0f, chapter.AfkRewardMultiplier),

                BaseRewards = ResolveBaseRewards(chapter, globalStage, localStage, chapterIndex),
                ClearRewards = ResolveClearRewards(chapter, globalStage, localStage, chapterIndex),

                EnemyScale = StageScalingFormula.GetEnemyScale(globalStage),
                BossScale = StageScalingFormula.GetBossScale(globalStage)
            };

            return runtimeData;
        }

        private StageReward[] ResolveBaseRewards(
            ChapterConfig chapter,
            int globalStage,
            int localStage,
            int chapterIndex
        )
        {
            RewardRule rule = FindRewardRule(chapter.RewardRuleId);
            if (rule == null || rule.BaseRewards == null)
                return Array.Empty<StageReward>();

            return EvaluateRewardEntries(rule.BaseRewards, chapter, globalStage, localStage, chapterIndex,
                applyAfkMultiplier: true);
        }

        private StageReward[] ResolveClearRewards(
            ChapterConfig chapter,
            int globalStage,
            int localStage,
            int chapterIndex
        )
        {
            RewardRule rule = FindRewardRule(chapter.RewardRuleId);
            if (rule == null || rule.ClearRewards == null)
                return Array.Empty<StageReward>();

            return EvaluateRewardEntries(rule.ClearRewards, chapter, globalStage, localStage, chapterIndex,
                applyAfkMultiplier: false);
        }

        private StageReward[] EvaluateRewardEntries(
            RewardFormulaEntry[] entries,
            ChapterConfig chapter,
            int globalStage,
            int localStage,
            int chapterIndex,
            bool applyAfkMultiplier
        )
        {
            if (entries == null || entries.Length == 0)
                return Array.Empty<StageReward>();

            StageFormulaContext context = new StageFormulaContext
            {
                GlobalStage = globalStage,
                LocalStage = localStage,
                ChapterId = chapter.ChapterId,
                ChapterIndex = chapterIndex
            };

            List<StageReward> result = new List<StageReward>();

            for (int i = 0; i < entries.Length; i++)
            {
                RewardFormulaEntry entry = entries[i];

                if (entry == null || string.IsNullOrWhiteSpace(entry.ResourceType))
                    continue;

                double amount = StageFormulaEvaluator.EvaluateDouble(
                    entry.Formula,
                    context,
                    0d
                );

                if (applyAfkMultiplier)
                    amount *= Mathf.Max(0f, chapter.AfkRewardMultiplier);

                amount = Math.Max(0d, amount);

                StageReward reward = new StageReward(
                    entry.ResourceType.Trim(),
                    amount
                );

                if (reward.IsValid)
                    result.Add(reward);
            }

            return result.ToArray();
        }

        private RewardRule FindRewardRule(string rewardRuleId)
        {
            if (rewardRuleConfig == null || rewardRuleConfig.Rules == null)
                return null;

            if (!string.IsNullOrWhiteSpace(rewardRuleId))
            {
                for (int i = 0; i < rewardRuleConfig.Rules.Length; i++)
                {
                    RewardRule rule = rewardRuleConfig.Rules[i];
                    if (rule != null && rule.RewardRuleId == rewardRuleId)
                        return rule;
                }
            }

            return rewardRuleConfig.Rules.Length > 0
                ? rewardRuleConfig.Rules[0]
                : null;
        }

        private bool TryResolveChapter(
            int globalStage,
            out ChapterConfig chapter,
            out int chapterIndex,
            out int localStage
        )
        {
            chapter = null;
            chapterIndex = -1;
            localStage = -1;

            if (chapterConfig == null || chapterConfig.Chapters == null || chapterConfig.Chapters.Length == 0)
                return false;

            int accumulatedStage = 0;

            for (int i = 0; i < chapterConfig.Chapters.Length; i++)
            {
                ChapterConfig data = chapterConfig.Chapters[i];
                if (data == null || data.StageCount <= 0)
                    continue;

                int chapterStartStage = accumulatedStage + 1;
                int chapterEndStage = accumulatedStage + data.StageCount;

                if (globalStage >= chapterStartStage && globalStage <= chapterEndStage)
                {
                    chapter = data;
                    chapterIndex = i;
                    localStage = globalStage - accumulatedStage;
                    return true;
                }

                accumulatedStage = chapterEndStage;
            }

            // Nếu stage vượt quá config hiện tại, dùng chapter cuối cùng làm loop/fallback.
            ChapterConfig lastChapter = chapterConfig.Chapters[chapterConfig.Chapters.Length - 1];
            if (lastChapter == null || lastChapter.StageCount <= 0)
                return false;

            chapter = lastChapter;
            chapterIndex = chapterConfig.Chapters.Length - 1;

            int overflowStage = globalStage - accumulatedStage;
            localStage = ((overflowStage - 1) % lastChapter.StageCount) + 1;

            return true;
        }

        private EnemyPatternRule FindEnemyRule(string ruleId)
        {
            if (enemyPatternRuleConfig == null || enemyPatternRuleConfig.Rules == null)
                return null;

            for (int i = 0; i < enemyPatternRuleConfig.Rules.Length; i++)
            {
                EnemyPatternRule rule = enemyPatternRuleConfig.Rules[i];
                if (rule != null && rule.RuleId == ruleId)
                    return rule;
            }

            return null;
        }

        private EnemyPatternData ResolveEnemyPattern(EnemyPatternRule rule, int localStage)
        {
            if (rule == null || rule.PatternLoopIds == null || rule.PatternLoopIds.Length == 0)
                return null;

            int index = (localStage - 1) % rule.PatternLoopIds.Length;
            string patternId = rule.PatternLoopIds[index];

            return FindEnemyPattern(patternId);
        }

        private EnemyPatternData FindEnemyPattern(string patternId)
        {
            if (enemyPatternRuleConfig == null || enemyPatternRuleConfig.Patterns == null)
                return null;

            for (int i = 0; i < enemyPatternRuleConfig.Patterns.Length; i++)
            {
                EnemyPatternData pattern = enemyPatternRuleConfig.Patterns[i];
                if (pattern != null && pattern.PatternId == patternId)
                    return pattern;
            }

            return null;
        }

        private BossPatternRule FindBossRule(string ruleId)
        {
            if (bossPatternRuleConfig == null || bossPatternRuleConfig.Rules == null)
                return null;

            for (int i = 0; i < bossPatternRuleConfig.Rules.Length; i++)
            {
                BossPatternRule rule = bossPatternRuleConfig.Rules[i];
                if (rule != null && rule.RuleId == ruleId)
                    return rule;
            }

            return null;
        }

        private int ResolveBossId(BossPatternRule rule, int localStage)
        {
            if (rule == null || rule.BossLoopIds == null || rule.BossLoopIds.Length == 0)
                return 0;

            int stagesPerBoss = Mathf.Max(1, rule.StagesPerBoss);
            int bossIndex = ((localStage - 1) / stagesPerBoss) % rule.BossLoopIds.Length;

            return rule.BossLoopIds[bossIndex];
        }

        private float[] NormalizeRates(float[] source)
        {
            if (source == null || source.Length == 0)
                return null;

            float[] result = new float[source.Length];

            float sum = 0f;
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = Mathf.Max(0f, source[i]);
                sum += result[i];
            }

            if (sum <= 0f)
            {
                float equal = 1f / source.Length;
                for (int i = 0; i < result.Length; i++)
                    result[i] = equal;

                return result;
            }

            for (int i = 0; i < result.Length; i++)
                result[i] /= sum;

            return result;
        }
    }
}