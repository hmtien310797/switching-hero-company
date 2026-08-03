using System;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Data
{
    /// <summary>
    /// Cấu hình trigger + runtime của 1 buff (DOCX §18 — Trigger + Condition + Runtime:
    /// Duration/InternalCooldown/StackMode/OncePerBattle/MaximumCount).
    /// </summary>
    [Serializable]
    public sealed class FormationBuffTriggerData
    {
        public FormationBuffTriggerType TriggerType = FormationBuffTriggerType.Passive;

        [Range(0f, 100f)] public float ChancePercent = 100f;   // Condition: chance to trigger.

        [Range(0f, 100f)] public float HpThresholdPercent = 30f;  // Condition: for HpBelowThreshold.

        // Runtime (DOCX §18).
        public float Duration;            // 0 = instant.
        public float InternalCooldown;    // action-counts giữa 2 lần trigger.
        public bool OncePerBattle;
        public int MaximumCount;          // 0 = unlimited.
    }
}
