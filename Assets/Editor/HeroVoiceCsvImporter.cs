#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Immortal_Switch.Scripts.Hero;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Map cột HeroNameEN trong hero_sound_config.csv vào 3 trường voice của HeroDataSO:
///   - Cột "Aoyi(1,het)" → ThirdHitVoiceKey (đòn đánh thứ 3)
///   - UltimateVoiceKey / PassiveVoiceKey = 1 sound bất kỳ trong cột Aoyi (dùng chính Aoyi của hero đó)
/// Chạy: Tools/Game Data/Hero Voice Importer
/// </summary>
public static class HeroVoiceCsvImporter
{
    private const string CsvPath = "Assets/Immortal Switch/CharacterVoice/hero_sound_config.csv";
    private const string HeroDataFolder = "Assets/Immortal Switch/Addressable/Hero/Data";

    /// <summary>
    /// Các hero không khớp được bằng tên (chuẩn hóa) sẽ dùng override thủ công.
    /// Key = HeroDataSO.Name, value = giá trị cột Aoyi (để trống = không có voice).
    /// </summary>
    private static readonly Dictionary<string, string> ManualOverrides = new()
    {
        ["Jerolur"] = "1005304_skilltalk1", // CSV dùng tên "Geironul" / "Jellorul" (model hero_jerolul)
        // ["Vorth"] = "" // không có voice trong CSV → bỏ trống
    };

    [MenuItem("Tools/Game Data/Hero Voice Importer")]
    public static void Import()
    {
        string content = ReadCsvContent();

        if (content == null)
        {
            Debug.LogError($"[HeroVoice] Không đọc được CSV: {CsvPath}");
            return;
        }

        List<List<string>> rows = ParseCsv(content);

        if (rows.Count < 2)
        {
            Debug.LogError("[HeroVoice] CSV rỗng hoặc chỉ có header.");
            return;
        }

        Dictionary<string, int> headers = BuildHeaderIndex(rows[0]);

        if (!headers.TryGetValue("heronameen", out int nameCol) ||
            !headers.TryGetValue("soundcount", out int countCol) ||
            !headers.TryGetValue("aoyi1het", out int aoyiCol))
        {
            Debug.LogError("[HeroVoice] CSV thiếu cột HeroNameEN / SoundCount / Aoyi(1,het).");
            return;
        }

        // Build map tên CSV chuẩn hóa → aoyi (chỉ dòng có voice).
        List<(string normalizedCsvName, string aoyi)> csvRows = new();

        for (int i = 1; i < rows.Count; i++)
        {
            List<string> row = rows[i];

            if (row == null || row.Count == 0)
                continue;

            string soundCountText = GetCell(row, countCol);
            if (string.IsNullOrWhiteSpace(soundCountText))
                continue;

            if (!int.TryParse(soundCountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int soundCount) ||
                soundCount == 0)
            {
                continue;
            }

            string name = GetCell(row, nameCol);
            if (string.IsNullOrWhiteSpace(name))
                continue;

            string aoyi = GetCell(row, aoyiCol);
            if (string.IsNullOrWhiteSpace(aoyi))
                continue;

            csvRows.Add((NormalizeName(name), aoyi));
        }

        // Cập nhật từng HeroDataSO.
        string[] guids = AssetDatabase.FindAssets("t:HeroDataSO", new[] { HeroDataFolder });

        int updatedCount = 0;
        int skippedCount = 0;
        List<string> log = new();

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            HeroDataSO heroData = AssetDatabase.LoadAssetAtPath<HeroDataSO>(assetPath);

            if (heroData == null)
                continue;

            string aoyi = ResolveAoyi(heroData.Name, csvRows);

            Undo.RecordObject(heroData, $"Set Hero Voice Keys: {heroData.Name}");

            heroData.ThirdHitVoiceKey = aoyi;
            heroData.UltimateVoiceKey = aoyi;
            heroData.PassiveVoiceKey = aoyi;

            EditorUtility.SetDirty(heroData);

            if (string.IsNullOrEmpty(aoyi))
            {
                skippedCount++;
                log.Add($"{heroData.Name} → (không có voice, để trống)");
            }
            else
            {
                updatedCount++;
                log.Add($"{heroData.Name} → {aoyi}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[HeroVoice] Import xong.\n" +
            string.Join("\n", log) +
            $"\nUpdated: {updatedCount}, NoVoice: {skippedCount}");
    }

    private static string ResolveAoyi(
        string heroName,
        List<(string normalizedCsvName, string aoyi)> csvRows)
    {
        if (string.IsNullOrEmpty(heroName))
            return string.Empty;

        if (ManualOverrides.TryGetValue(heroName, out string overrideValue))
            return overrideValue;

        string normalized = NormalizeName(heroName);

        // 1) Khớp chính xác.
        for (int i = 0; i < csvRows.Count; i++)
        {
            if (csvRows[i].normalizedCsvName == normalized)
                return csvRows[i].aoyi;
        }

        // 2) Tên hero là prefix / chứa trong tên CSV (vd "edward" ⊂ "edwardrackham").
        //    Chọn ứng viên có name CSV ngắn nhất chứa normalized (ưu tiên chính xác hơn).
        string best = null;
        int bestLen = int.MaxValue;

        for (int i = 0; i < csvRows.Count; i++)
        {
            string csvName = csvRows[i].normalizedCsvName;
            if (csvName.Contains(normalized) && csvName.Length < bestLen)
            {
                best = csvRows[i].aoyi;
                bestLen = csvName.Length;
            }
        }

        return best ?? string.Empty;
    }

    private static string NormalizeName(string value)
    {
        StringBuilder sb = new();
        foreach (char c in value)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    private static Dictionary<string, int> BuildHeaderIndex(IReadOnlyList<string> headerRow)
    {
        Dictionary<string, int> map = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headerRow.Count; i++)
        {
            string normalized = NormalizeName(headerRow[i]?.Trim().TrimStart('﻿') ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(normalized))
                map[normalized] = i;
        }

        return map;
    }

    private static string GetCell(IReadOnlyList<string> row, int index)
    {
        return index >= 0 && index < row.Count ? (row[index] ?? string.Empty).Trim() : string.Empty;
    }

    /// <summary>
    /// Đọc nội dung CSV một cách an toàn với Unity Editor.
    /// Ưu tiên AssetDatabase.LoadAssetAtPath (Unity giữ file → tránh Sharing violation),
    /// fallback đọc trực tiếp với FileShare.ReadWrite + retry khi file đang bị khóa tại thời điểm đó.
    /// </summary>
    private static string ReadCsvContent()
    {
        if (!File.Exists(CsvPath))
            return null;

        // Cách chuẩn: đọc qua cache hạ tầng của Unity (CSV được import như TextAsset).
        TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(CsvPath);
        if (textAsset != null)
            return textAsset.text;

        // Fallback cho trường hợp file vừa thay đổi ngoài editor, import chưa kịp chạy.
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                using FileStream fs = new(CsvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader reader = new(fs, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch (IOException)
            {
                System.Threading.Thread.Sleep(200);
            }
        }

        return null;
    }

    private static List<List<string>> ParseCsv(string content)
    {
        List<List<string>> rows = new();
        List<string> currentRow = new();
        StringBuilder currentCell = new();

        bool isInsideQuotes = false;

        for (int i = 0; i < content.Length; i++)
        {
            char character = content[i];

            if (character == '"')
            {
                bool isEscapedQuote = isInsideQuotes && i + 1 < content.Length && content[i + 1] == '"';

                if (isEscapedQuote)
                {
                    currentCell.Append('"');
                    i++;
                }
                else
                {
                    isInsideQuotes = !isInsideQuotes;
                }

                continue;
            }

            if (character == ',' && !isInsideQuotes)
            {
                currentRow.Add(currentCell.ToString());
                currentCell.Clear();
                continue;
            }

            bool isNewLine = character == '\r' || character == '\n';

            if (isNewLine && !isInsideQuotes)
            {
                if (character == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                    i++;

                currentRow.Add(currentCell.ToString());
                currentCell.Clear();

                rows.Add(currentRow);
                currentRow = new List<string>();
                continue;
            }

            currentCell.Append(character);
        }

        currentRow.Add(currentCell.ToString());
        rows.Add(currentRow);

        return rows;
    }
}

#endif