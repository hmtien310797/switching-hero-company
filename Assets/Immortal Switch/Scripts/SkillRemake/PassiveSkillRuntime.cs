using System.Collections.Generic;
using UnityEngine;
using Battle;

namespace Immortal_Switch.Scripts.Skill
{
    public sealed class PassiveSkillRuntime
    {
        private readonly HeroSkillController ownerController;
        private readonly SkillDataSO skillData;
        private readonly int level;
        private readonly SkillPassiveConfig config;
        private readonly Dictionary<string, int> stacks = new();

        public PassiveSkillRuntime(HeroSkillController ownerController, SkillDataSO skillData, int level)
        {
            this.ownerController = ownerController;
            this.skillData = skillData;
            this.level = level;
            config = skillData != null ? skillData.GetPassiveConfig(level) : null;
        }

        public void Reset()
        {
            stacks.Clear();
        }

        public void HandleEvent(SkillEventContext eventContext)
        {
            if (eventContext == null || skillData == null || config == null)
                return;

            if (config.StackGainTriggers == null || config.StackGainTriggers.Count == 0)
                return;

            for (int i = 0; i < config.StackGainTriggers.Count; i++)
            {
                SkillTriggerStackGainData trigger = config.StackGainTriggers[i];
                if (trigger == null || trigger.EventType != eventContext.EventType)
                    continue;

                if (!PassSourceFilter(trigger.SourceFilter, eventContext))
                    continue;

                AddStack(trigger.StackGainAmount);
            }
        }

        private bool PassSourceFilter(SkillEventSourceFilter filter, SkillEventContext eventContext)
        {
            HeroActor owner = ownerController != null ? ownerController.Owner : null;
            if (owner == null)
                return false;

            switch (filter)
            {
                case SkillEventSourceFilter.Owner:
                    return eventContext.Source == owner;

                case SkillEventSourceFilter.Ally:
                    // Ally ở phase 1 được hiểu là hero khác cùng phe, không tính enemy.
                    return eventContext.Source is HeroActor && eventContext.Source != owner;

                case SkillEventSourceFilter.OwnerAndAlly:
                    // Owner + ally chỉ nhận event từ hero phe mình, không nhận event từ enemy.
                    return eventContext.Source is HeroActor;

                default:
                    return false;
            }
        }

        private void AddStack(int amount)
        {
            string key = string.IsNullOrEmpty(config.StackKey) ? skillData.SkillKey : config.StackKey;
            if (string.IsNullOrEmpty(key))
                key = skillData.name;

            stacks.TryGetValue(key, out int current);
            current = Mathf.Clamp(current + Mathf.Max(1, amount), 0, Mathf.Max(1, config.MaxStack));
            stacks[key] = current;

            if (current >= Mathf.Max(1, config.RequiredStack) && PassEnemyCountCondition())
                Trigger(key);
        }

        private bool PassEnemyCountCondition()
        {
            if (config.EnemyCountCondition == null || !config.EnemyCountCondition.Enabled)
                return true;

            return ownerController != null &&
                   ownerController.CountEnemiesInRange(config.EnemyCountCondition.Range) >= config.EnemyCountCondition.MinEnemyCount;
        }

        private void Trigger(string key)
        {
            if (config.ResetStackOnTrigger)
                stacks[key] = 0;
            else if (config.ConsumeStackOnTrigger)
                stacks[key] = Mathf.Max(0, stacks[key] - config.RequiredStack);

            ownerController.CastPassiveTriggeredSkill(skillData);
        }
    }
}
