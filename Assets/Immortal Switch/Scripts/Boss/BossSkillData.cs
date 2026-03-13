using System;
using System.Collections.Generic;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    [Serializable]
    public class BossSkillData
    {
        public int SkillId;
        public string SkillName;
        [TextArea(3, 8)] public string Description;

        public BossSkillType SkillType;
        public BossSkillTriggerData Trigger;
        public SkillTargetType TargetType;

        [Tooltip("Số target ngẫu nhiên hoặc số tia sét, nếu có")]
        public int TargetCount = 1;

        public List<SkillEffectData> Effects = new();
    }
    
    public enum BossSkillType
    {
        Passive,
        Active
    }
    
    public enum SkillTargetType
    {
        CurrentTarget,
        Self,
        AllEnemies,
        AllAllies,
        RandomEnemy,
        LowestHpEnemy,
        HighestHpEnemy,
        AreaAroundTarget,
        AreaAroundSelf
    }
}