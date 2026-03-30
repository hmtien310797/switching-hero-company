using System.Text.RegularExpressions;

namespace Immortal_Switch.Scripts.Skill.UI
{
    public static class SkillDescriptionFormatter
    {
        // Màu nổi cho số liệu skill
        private const string HighlightColor = "#FFA94D";

        /// <summary>
        /// Build description từ SkillDataSO rồi format đẹp cho TMP.
        /// </summary>
        public static string BuildFormattedDescription(Immortal_Switch.Scripts.Skill.SkillDataSO skillData, int level)
        {
            if (skillData == null)
                return string.Empty;

            string raw = skillData.BuildDescription(level);
            return FormatForTMP(raw);
        }

        /// <summary>
        /// Format text thô thành rich text đẹp hơn cho TMP.
        /// </summary>
        public static string FormatForTMP(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            string result = raw.Trim();

            // Chuẩn hóa line break
            result = result.Replace("\r\n", "\n");

            // Highlight số % ATK, ví dụ 324% ATK
            result = Regex.Replace(
                result,
                @"(\d+(\.\d+)?)%(\s*)ATK",
                m => WrapHighlight($"{m.Groups[1].Value}% ATK"),
                RegexOptions.IgnoreCase
            );

            // Highlight số % thường, ví dụ 20%
            result = Regex.Replace(
                result,
                @"(?<![>#])(\d+(\.\d+)?)%",
                m => WrapHighlight($"{m.Groups[1].Value}%")
            );

            // Highlight khoảng cách, ví dụ 2.5m
            result = Regex.Replace(
                result,
                @"(\d+(\.\d+)?)m",
                m => WrapHighlight($"{m.Groups[1].Value}m"),
                RegexOptions.IgnoreCase
            );

            // Highlight số lần đánh, ví dụ 10 lần / 10 times
            result = Regex.Replace(
                result,
                @"\b(\d+)\s*(lần|times|targets|mục tiêu)\b",
                m => WrapHighlight($"{m.Groups[1].Value} {m.Groups[2].Value}"),
                RegexOptions.IgnoreCase
            );

            return result;
        }

        private static string WrapHighlight(string value)
        {
            return $"<color={HighlightColor}><b>{value}</b></color>";
        }
    }
}