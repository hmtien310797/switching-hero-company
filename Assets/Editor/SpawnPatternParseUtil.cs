#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Immortal_Switch.Scripts.EditorTools
{
    public static class SpawnPatternParseUtil
    {
        public static List<(int id, int[] enemyIds)> ParseRows(string text)
        {
            var result = new List<(int id, int[] enemyIds)>();

            var lines = text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            if (lines.Count == 0) return result;

            int startIndex = LooksLikeHeader(lines[0]) ? 1 : 0;

            for (int i = startIndex; i < lines.Count; i++)
            {
                var line = lines[i];
                var cols = SplitColumns(line);

                if (cols.Length < 2)
                {
                    Debug.LogWarning($"Line {i + 1}: expected 2 columns, got {cols.Length}. Skipped. Line='{line}'");
                    continue;
                }

                if (!int.TryParse(cols[0].Trim(), out var id))
                {
                    Debug.LogWarning($"Line {i + 1}: invalid id '{cols[0]}'. Skipped.");
                    continue;
                }

                var enemyIds = ParseEnemyIds(cols[1]);

                if (enemyIds.Length == 0)
                {
                    Debug.LogWarning($"Line {i + 1}: pattern empty/invalid. Skipped.");
                    continue;
                }

                result.Add((id: id, enemyIds: enemyIds));
            }

            // sort by id
            result.Sort((a, b) => a.id.CompareTo(b.id));
            return result;
        }

        private static bool LooksLikeHeader(string line)
        {
            var low = line.ToLowerInvariant();
            return low.Contains("id") && low.Contains("pattern");
        }

        private static string[] SplitColumns(string line)
        {
            if (line.Contains('\t'))
                return line.Split('\t');

            if (line.Contains(','))
                return line.Split(',');

            return line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static int[] ParseEnemyIds(string patternStr)
        {
            var parts = patternStr
                .Trim()
                .Split(new[] { ';', '|'}, StringSplitOptions.RemoveEmptyEntries);

            var list = new List<int>(parts.Length);
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out var v))
                    list.Add(v);
            }

            return list.Distinct().ToArray();
        }
    }
}
#endif