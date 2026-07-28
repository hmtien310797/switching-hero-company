using System;
using System.Globalization;
using System.Text;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;

namespace Immortal_Switch.Scripts.SkillSystem.Description
{
    /// <summary>
    /// Build description cho skill sử dụng Spine GameObject.
    ///
    /// Cách hoạt động:
    /// - Đọc description template từ LocalizationManager theo <see cref="SkillDataSO.DescriptionKey"/>.
    /// - Duyệt qua <see cref="ClassSkillDescriptionLevelValues"/> array, mỗi phần tử tương ứng
    ///   với một placeholder <c>{0}</c>, <c>{1}</c>, <c>{2}</c>... trong template.
    /// - Nếu <see cref="ClassSkillDescriptionLevelValues.ScaleWithSkillLevel"/> = true,
    ///   giá trị sẽ được scale theo <see cref="SkillDataSO.ClassSkillScaling"/>.
    /// - Format giá trị và replace placeholder trong template.
    /// </summary>
    public static class SpineSkillDescriptionBuilder
    {
        /// <summary>
        /// Build description hoàn chỉnh của Spine Skill tại level hiện tại,
        /// sử dụng <paramref name="classSkillDescriptionLevelValues"/> để thay thế
        /// các placeholder <c>{0}</c>, <c>{1}</c>, <c>{2}</c>... trong template.
        /// </summary>
        public static string Build(
            SkillDataSO skillData,
            int currentSkillLevel,
            ClassSkillDescriptionLevelValues[] classSkillDescriptionLevelValues)
        {
            if (skillData == null)
            {
                Debug.LogWarning(
                    "[SpineSkillDescriptionBuilder] SkillDataSO is null.");

                return string.Empty;
            }

            string descriptionTemplate = LocalizationManager.GetText(skillData.DescriptionKey);

            if (string.IsNullOrWhiteSpace(descriptionTemplate))
                return string.Empty;

            currentSkillLevel = Mathf.Max(1, currentSkillLevel);

            StringBuilder builder = new StringBuilder(descriptionTemplate);

            if (classSkillDescriptionLevelValues != null)
            {
                for (int i = 0; i < classSkillDescriptionLevelValues.Length; i++)
                {
                    ClassSkillDescriptionLevelValues levelValue =
                        classSkillDescriptionLevelValues[i];

                    if (levelValue == null)
                        continue;

                    string placeholder = $"{{{i}}}";

                    if (builder.ToString().IndexOf(
                            placeholder,
                            StringComparison.Ordinal) < 0)
                    {
                        continue;
                    }

                    float value = levelValue.Values;

                    if (levelValue.ScaleWithSkillLevel)
                    {
                        value = skillData.GetScaledClassSkillValue(
                            value,
                            currentSkillLevel,
                            true);
                    }

                    string formatted = FormatPercent(value);

                    ReplaceIgnoreCase(builder, placeholder, formatted);
                }
            }

            return builder.ToString();
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
    }
}
