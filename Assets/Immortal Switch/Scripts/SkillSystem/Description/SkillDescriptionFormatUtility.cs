using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Immortal_Switch.Scripts.SkillSystem.Description
{
    /// <summary>
    /// Đếm placeholder dạng <c>{0}</c>, <c>{1}</c>, <c>{0:F2}</c>... và format description
    /// một cách an toàn cho <see cref="SkillDataSO"/>.
    ///
    /// Parser cố tình chỉ nhận placeholder số để không nhầm với:
    /// - Rich text tag của TextMeshPro (<c>&lt;color=#FFF&gt;</c>).
    /// - Token đặc thù của Spine builder (<c>{hit}</c>, <c>{finalhit}</c>).
    /// - Curly brace được escape (<c>{{0}}</c>).
    /// </summary>
    public static class SkillDescriptionFormatUtility
    {
        // Match {digits} hoặc {digits:format}, ví dụ {0}, {1}, {0:F2}, {10:N1}.
        // Negative lookbehind (?<!\{) để bỏ qua escaped {{0}}.
        private static readonly Regex NumericPlaceholderRegex =
            new Regex(
                @"(?<!\{)\{(\d+)(?::[^}]*)?\}",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Số lượng argument cần thiết cho template.
        /// Công thức: <c>maxPlaceholderIndex + 1</c>.
        /// Trả về <c>0</c> nếu không có placeholder.
        /// </summary>
        public static int GetRequiredArgumentCount(string template)
        {
            if (string.IsNullOrEmpty(template))
                return 0;

            int maxIndex = -1;

            MatchCollection matches = NumericPlaceholderRegex.Matches(template);
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                if (!match.Success || match.Groups.Count < 2)
                    continue;

                Group indexGroup = match.Groups[1];
                if (!indexGroup.Success || indexGroup.Value.Length == 0)
                    continue;

                if (!int.TryParse(
                        indexGroup.Value,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out int index))
                {
                    continue;
                }

                if (index > maxIndex)
                    maxIndex = index;
            }

            return maxIndex < 0 ? 0 : maxIndex + 1;
        }

        /// <summary>
        /// Format template bằng <paramref name="valueKey"/> an toàn:
        /// - Pad thiếu bằng <c>0f</c>, bỏ phần tử thừa.
        /// - Dùng <see cref="CultureInfo.InvariantCulture"/> để tránh dấu phẩy thập phân.
        /// - Không tự làm tròn số nguyên.
        /// - Nếu format lỗi, trả về template gốc (không throw).
        /// - <c>null</c> template trả về <see cref="string.Empty"/>.
        /// </summary>
        public static string FormatDescription(string template, float[] valueKey)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            int required = GetRequiredArgumentCount(template);
            if (required == 0)
                return template;

            object[] args = new object[required];
            int available = valueKey != null ? valueKey.Length : 0;

            for (int i = 0; i < required; i++)
            {
                args[i] = i < available ? (object)valueKey[i] : 0f;
            }

            try
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    template,
                    args);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[SkillDescriptionFormatUtility] Format failed. " +
                    $"Template='{template}'. {exception.Message}");

                return template;
            }
        }
    }
}
