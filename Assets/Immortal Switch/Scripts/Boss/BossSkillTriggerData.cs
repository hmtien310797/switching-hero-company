using System;
using UnityEngine;

namespace Immortal_Switch.Scripts.Boss
{
    [Serializable]
    public class BossSkillTriggerData
    {
        public BossSkillTriggerType TriggerType;

        [Tooltip("Dùng cho HP threshold. Ví dụ 70 = 70%")]
        public float PercentValue;

        [Tooltip("Dùng cho số đòn đánh / số hit / số stack")]
        public int CountValue;

        [Tooltip("Phần trăm kích hoạt, ví dụ 30 = 30%")]
        [Range(0, 100)]
        public float ProcChance;

        [Tooltip("Cooldown theo giây")]
        public float Cooldown;

        [Tooltip("Chỉ kích hoạt 1 lần")]
        public bool TriggerOnce = true;
    }
    
    public enum BossSkillTriggerType
    {
        None,
        OnBattleStart,
        OnHpBelowPercent,
        OnHpLostStepPercent,
        OnNormalAttackCountReached,
        OnAttackPerformed,
        OnSkillCast,
        OnActionPerformed,
        OnHitTaken,
        OnHitTakenCountReached,
        OnTargetMarkStackReached
    }
}