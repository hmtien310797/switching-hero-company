using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;

namespace Immortal_Switch.Scripts.SkillSystem.Description
{
    /// <summary>
    /// Build description cho skill sử dụng Spine GameObject.
    ///
    /// Các parameter đang hỗ trợ:
    /// {hit}      : Phase có event hit, hit1, hit2...
    /// {finalhit} : Phase có event finalhit
    /// </summary>
    public static class SpineSkillDescriptionBuilder
    {
        private const string HitParameter = "{hit}";
        private const string FinalHitParameter = "{finalhit}";

        private const string HitEventPrefix = "hit";
        private const string FinalHitEventName = "finalhit";

        /// <summary>
        /// Build description hoàn chỉnh của Spine Skill tại level hiện tại.
        /// </summary>
        public static string Build(
            SkillDataSO skillData,
            int currentSkillLevel,
            Color hitColor,
            Color finalHitColor)
        {
            if (skillData == null)
            {
                Debug.LogWarning(
                    "[SpineSkillDescriptionBuilder] SkillDataSO is null.");

                return string.Empty;
            }

            string descriptionTemplate = skillData.Description;

            if (string.IsNullOrWhiteSpace(descriptionTemplate))
                return string.Empty;

            currentSkillLevel = Mathf.Max(1, currentSkillLevel);

            StringBuilder builder = new StringBuilder(descriptionTemplate);

            ReplaceDamageParameter(
                builder,
                skillData,
                currentSkillLevel,
                HitParameter,
                HitEventPrefix,
                EventSearchMode.HitPhase,
                hitColor);

            ReplaceDamageParameter(
                builder,
                skillData,
                currentSkillLevel,
                FinalHitParameter,
                FinalHitEventName,
                EventSearchMode.Exact,
                finalHitColor);

            return builder.ToString();
        }

        private static void ReplaceDamageParameter(
            StringBuilder builder,
            SkillDataSO skillData,
            int currentSkillLevel,
            string parameter,
            string targetEventName,
            EventSearchMode searchMode,
            Color valueColor)
        {
            string currentDescription = builder.ToString();

            if (currentDescription.IndexOf(
                    parameter,
                    StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            if (!TryGetDamagePercent(
                    skillData,
                    currentSkillLevel,
                    targetEventName,
                    searchMode,
                    out float damagePercent))
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"[SpineSkillDescriptionBuilder] Không tìm thấy damage cho " +
                    $"parameter '{parameter}'. " +
                    $"Skill: '{skillData.name}', " +
                    $"Level: {currentSkillLevel}, " +
                    $"Owner type: {skillData.OwnerType}, " +
                    $"Event: '{targetEventName}'.");
#endif
                return;
            }

            string formattedDamage = FormatPercent(damagePercent);

            string coloredDamage = WrapColor(
                formattedDamage,
                valueColor);

            ReplaceIgnoreCase(
                builder,
                parameter,
                coloredDamage);
        }
        
        private static string WrapColor(
            string value,
            Color color)
        {
            string hexColor =
                ColorUtility.ToHtmlStringRGBA(color);

            return $"<color=#{hexColor}>{value}</color>";
        }
        
        private static bool TryGetDamagePercent(
            SkillDataSO skillData,
            int currentSkillLevel,
            string targetEventName,
            EventSearchMode searchMode,
            out float damagePercent)
        {
            damagePercent = 0f;

            switch (skillData.OwnerType)
            {
                case SkillOwnerType.ClassSkill:
                    return TryGetClassSkillDamagePercent(
                        skillData,
                        currentSkillLevel,
                        targetEventName,
                        searchMode,
                        out damagePercent);

                case SkillOwnerType.UltimateSkill:
                    return TryGetUltimateSkillDamagePercent(
                        skillData,
                        currentSkillLevel,
                        targetEventName,
                        searchMode,
                        out damagePercent);

                default:
#if UNITY_EDITOR
                    Debug.LogWarning(
                        $"[SpineSkillDescriptionBuilder] OwnerType " +
                        $"'{skillData.OwnerType}' chưa được hỗ trợ. " +
                        $"Skill: '{skillData.name}'.");
#endif
                    return false;
            }
        }
        
        private static bool TryGetClassSkillDamagePercent(
            SkillDataSO skillData,
            int currentSkillLevel,
            string targetEventName,
            EventSearchMode searchMode,
            out float damagePercent)
        {
            damagePercent = 0f;

            if (!TryGetDamageFromPhases(
                    skillData.BasePhases,
                    targetEventName,
                    searchMode,
                    out float baseDamagePercent))
            {
                return false;
            }

            damagePercent = CalculateClassSkillDamageByLevel(
                skillData,
                baseDamagePercent,
                currentSkillLevel);

            return true;
        }
        
        private static float CalculateClassSkillDamageByLevel(
            SkillDataSO skillData,
            float baseDamagePercent,
            int currentSkillLevel)
        {
            currentSkillLevel = Mathf.Max(1, currentSkillLevel);

            if (skillData.ClassSkillScaling == null)
                return baseDamagePercent;

            float growthPercentPerLevel =
                skillData.ClassSkillScaling.GrowthPercentPerLevel;

            int additionalLevel = currentSkillLevel - 1;

            return baseDamagePercent *
                   (1f + additionalLevel * growthPercentPerLevel / 100f);
        }
        
        private static bool TryGetUltimateSkillDamagePercent(
            SkillDataSO skillData,
            int currentSkillLevel,
            string targetEventName,
            EventSearchMode searchMode,
            out float damagePercent)
        {
            damagePercent = 0f;

            if (skillData.Levels == null ||
                skillData.Levels.Count == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"[SpineSkillDescriptionBuilder] Ultimate Skill " +
                    $"'{skillData.name}' không có Levels data.");
#endif
                return false;
            }

            int levelIndex = Mathf.Clamp(
                currentSkillLevel - 1,
                0,
                skillData.Levels.Count - 1);

            var levelData = skillData.Levels[levelIndex];

            if (levelData == null)
                return false;

            return TryGetDamageFromPhases(
                levelData.Phases,
                targetEventName,
                searchMode,
                out damagePercent);
        }
        
        private static bool TryGetDamageFromPhases(
            List<SkillPhaseData> phases,
            string targetEventName,
            EventSearchMode searchMode,
            out float damagePercent)
        {
            damagePercent = 0f;

            if (phases == null || phases.Count == 0)
                return false;

            for (int phaseIndex = 0;
                 phaseIndex < phases.Count;
                 phaseIndex++)
            {
                SkillPhaseData phase = phases[phaseIndex];

                if (phase == null)
                    continue;

                if (!IsMatchingEvent(
                        phase.EventName,
                        targetEventName,
                        searchMode))
                {
                    continue;
                }

                if (phase.Actions == null ||
                    phase.Actions.Count == 0)
                {
                    continue;
                }

                var firstAction = phase.Actions[0];

                if (firstAction == null ||
                    firstAction.Damage == null)
                {
                    continue;
                }

                damagePercent =
                    firstAction.Damage.SkillDamageBonusPercent;

                return true;
            }

            return false;
        }

        private static bool IsMatchingEvent(
            string eventName,
            string targetEventName,
            EventSearchMode searchMode)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                return false;

            eventName = eventName.Trim();

            switch (searchMode)
            {
                case EventSearchMode.Exact:
                    return string.Equals(
                        eventName,
                        targetEventName,
                        StringComparison.OrdinalIgnoreCase);

                case EventSearchMode.HitPhase:
                    return IsHitPhaseEvent(eventName);

                default:
                    return false;
            }
        }
        
        private static bool IsHitPhaseEvent(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                return false;

            eventName = eventName.Trim();

            if (string.Equals(
                    eventName,
                    HitEventPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!eventName.StartsWith(
                    HitEventPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string suffix = eventName.Substring(HitEventPrefix.Length);

            // Hỗ trợ hit_1, hit_2...
            if (suffix.StartsWith("_", StringComparison.Ordinal))
                suffix = suffix.Substring(1);

            if (string.IsNullOrEmpty(suffix))
                return false;

            // Chỉ nhận hit1, hit2, hit10...
            for (int i = 0; i < suffix.Length; i++)
            {
                if (!char.IsDigit(suffix[i]))
                    return false;
            }

            return true;
        }

        private static string FormatPercent(float value)
        {
            if (Mathf.Approximately(
                    value,
                    Mathf.Round(value)))
            {
                return Mathf.RoundToInt(value).ToString(
                    CultureInfo.InvariantCulture);
            }

            return value.ToString(
                "0.##",
                CultureInfo.InvariantCulture);
        }

        private static void ReplaceIgnoreCase(
            StringBuilder builder,
            string oldValue,
            string newValue)
        {
            string source = builder.ToString();

            int index = source.IndexOf(
                oldValue,
                StringComparison.OrdinalIgnoreCase);

            while (index >= 0)
            {
                builder.Remove(index, oldValue.Length);
                builder.Insert(index, newValue);

                source = builder.ToString();

                index = source.IndexOf(
                    oldValue,
                    index + newValue.Length,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        private enum EventSearchMode
        {
            Exact,
            HitPhase
        }
    }
}