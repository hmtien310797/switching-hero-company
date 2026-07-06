using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    public sealed class PassiveSkillRuntime
    {
        private readonly HeroSkillController ownerController;
        private readonly SkillDataSO skillData;
        private readonly int level;

        private readonly SkillPassiveConfig config;
        private readonly ResolvedPassiveConfig resolvedConfig;

        private readonly Dictionary<string, int> stacks = new();

        private string RuntimeBuffId =>
            $"passive_{skillData.SkillKey}_{skillData.SkillId}";

        public PassiveSkillRuntime(
            HeroSkillController ownerController,
            SkillDataSO skillData,
            int level)
        {
            this.ownerController = ownerController;
            this.skillData = skillData;
            this.level = level;

            resolvedConfig = skillData != null
                ? skillData.GetResolvedPassiveConfig(level)
                : null;

            config = resolvedConfig?.BaseConfig;
        }

        public void Reset()
        {
            stacks.Clear();

            HeroActor owner = ownerController != null
                ? ownerController.Owner
                : null;

            owner?.Stats?.BuffModule?.RemoveBuffById(RuntimeBuffId);
        }

        public int GetCurrentStack()
        {
            if (config == null)
                return 0;

            string key = GetStackKey();

            return stacks.TryGetValue(key, out int value)
                ? value
                : 0;
        }

        public void HandleEvent(SkillEventContext eventContext)
        {
            if (eventContext == null ||
                skillData == null ||
                config == null)
            {
                return;
            }

            if (config.BlockStackGainDuringCooldown &&
                ownerController != null &&
                !ownerController.IsCooldownReady(skillData))
            {
                return;
            }

            if (config.StackGainTriggers == null ||
                config.StackGainTriggers.Count == 0)
            {
                return;
            }

            for (int i = 0;
                 i < config.StackGainTriggers.Count;
                 i++)
            {
                SkillTriggerStackGainData trigger =
                    config.StackGainTriggers[i];

                if (trigger == null ||
                    trigger.EventType != eventContext.EventType)
                {
                    continue;
                }

                if (!PassSourceFilter(
                        trigger.SourceFilter,
                        eventContext))
                {
                    continue;
                }

                if (!PassHitSourceFilter(
                        trigger.HitSourceFilter,
                        eventContext))
                {
                    continue;
                }

                AddStack(trigger.StackGainAmount);
            }
        }

        private bool PassSourceFilter(
            SkillEventSourceFilter filter,
            SkillEventContext eventContext)
        {
            HeroActor owner = ownerController != null
                ? ownerController.Owner
                : null;

            if (owner == null)
                return false;

            switch (filter)
            {
                case SkillEventSourceFilter.Owner:
                    return eventContext.Source == owner;

                case SkillEventSourceFilter.Ally:
                    return eventContext.Source is HeroActor &&
                           eventContext.Source != owner;

                case SkillEventSourceFilter.OwnerAndAlly:
                    return eventContext.Source is HeroActor;

                default:
                    return false;
            }
        }

        private static bool PassHitSourceFilter(
            PassiveHitSourceFilter filter,
            SkillEventContext eventContext)
        {
            switch (filter)
            {
                case PassiveHitSourceFilter.BasicAttackOnly:
                    // Quy ước:
                    // Basic Attack không mang SkillDataSO.
                    return eventContext.Skill == null;

                case PassiveHitSourceFilter.SkillOnly:
                    return eventContext.Skill != null;

                case PassiveHitSourceFilter.Any:
                default:
                    return true;
            }
        }

        private void AddStack(int amount)
        {
            if (amount <= 0)
                return;

            string key = GetStackKey();

            stacks.TryGetValue(key, out int current);

            int maxStack = Mathf.Max(1, config.MaxStack);

            current = Mathf.Clamp(
                current + amount,
                0,
                maxStack);

            stacks[key] = current;

            int requiredStack =
                Mathf.Max(1, config.RequiredStack);

            if (current < requiredStack)
                return;

            if (!PassEnemyCountCondition())
                return;

            Trigger(key);
        }

        private bool PassEnemyCountCondition()
        {
            if (config.EnemyCountCondition == null ||
                !config.EnemyCountCondition.Enabled)
            {
                return true;
            }

            if (ownerController == null)
                return false;

            int enemyCount =
                ownerController.CountEnemiesInRange(
                    config.EnemyCountCondition.Range);

            return enemyCount >=
                   config.EnemyCountCondition.MinEnemyCount;
        }

        private void Trigger(string key)
        {
            if (ownerController == null)
                return;

            bool activated =
                ownerController.TryActivatePassive(
                    skillData,
                    CreateBuffData());

            // Cooldown chưa sẵn sàng hoặc hero không hợp lệ:
            // không được reset stack.
            if (!activated)
                return;

            if (config.ResetStackOnTrigger)
            {
                stacks[key] = 0;
            }
            else if (config.ConsumeStackOnTrigger)
            {
                stacks[key] = Mathf.Max(
                    0,
                    stacks[key] -
                    Mathf.Max(1, config.RequiredStack));
            }
        }

        private BuffData CreateBuffData()
        {
            BuffData buffData = new BuffData
            {
                Id = RuntimeBuffId,
                Name = string.IsNullOrEmpty(skillData.SkillName)
                    ? skillData.name
                    : skillData.SkillName,
                Kind = BuffKind.Buff,
                Duration = Mathf.Max(0f, config.BuffDuration),
                MaxStacks = 1,
                StackRule = BuffStackRule.Replace,
                Modifiers = new List<StatModifier>()
            };

            if (resolvedConfig?.Modifiers == null)
                return buffData;

            for (int i = 0;
                 i < resolvedConfig.Modifiers.Count;
                 i++)
            {
                StatModifier modifier =
                    resolvedConfig.Modifiers[i];

                if (modifier == null)
                    continue;

                buffData.Modifiers.Add(modifier.Clone());
            }

            return buffData;
        }

        private string GetStackKey()
        {
            if (!string.IsNullOrEmpty(config.StackKey))
                return config.StackKey;

            if (!string.IsNullOrEmpty(skillData.SkillKey))
                return skillData.SkillKey;

            return skillData.name;
        }
    }
}