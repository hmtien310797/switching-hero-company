using System;
using System.Collections.Generic;
using System.IO;
using Editor.ExcelConfigTool.Models;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Editor.ExcelConfigTool.Services
{
    public static class ExcelConfigToolSettingsStore
    {
        public const string SETTINGS_PATH = "ProjectSettings/ExcelConfigToolSettings.json";

        private const string LEGACY_EDITOR_PREFS_KEY = "ExcelConfigTool_Entries";

        public static ExcelConfigToolSettings Load()
        {
            ExcelConfigToolSettings settings = null;

            if (File.Exists(SETTINGS_PATH))
            {
                try
                {
                    settings = JsonConvert.DeserializeObject<ExcelConfigToolSettings>(
                        File.ReadAllText(SETTINGS_PATH)
                    );
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ExcelConfigTool] Cannot read {SETTINGS_PATH}:\n{e}");
                }
            }

            settings ??= new ExcelConfigToolSettings();
            settings.entries ??= new List<UrlEntry>();

            var shouldSave = !File.Exists(SETTINGS_PATH);

            if (!File.Exists(SETTINGS_PATH))
            {
                MigrateLegacyEntries(settings);
            }

            if (settings.version < 2)
            {
                // Version 1 may contain display names shifted by failed downloads.
                foreach (var entry in settings.entries)
                {
                    entry.fileName = string.Empty;
                }

                settings.version = 2;
                shouldSave = true;
            }

            if (shouldSave)
            {
                Save(settings);
            }

            return settings;
        }

        public static void Save(ExcelConfigToolSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.entries ??= new List<UrlEntry>();

            if (settings.version < 2)
            {
                foreach (var entry in settings.entries)
                {
                    entry.fileName = string.Empty;
                }

                settings.version = 2;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(SETTINGS_PATH) ?? "ProjectSettings");

            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(SETTINGS_PATH, json);
        }

        private static void MigrateLegacyEntries(ExcelConfigToolSettings settings)
        {
            var raw = EditorPrefs.GetString(LEGACY_EDITOR_PREFS_KEY, string.Empty);

            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            try
            {
                settings.entries = JsonConvert.DeserializeObject<List<UrlEntry>>(raw) ?? new List<UrlEntry>();

                // Legacy display names may be shifted when an earlier URL failed.
                // Rebuild them from successful responses using SourceIndex.
                foreach (var entry in settings.entries)
                {
                    entry.fileName = string.Empty;
                }

                Debug.Log(
                    $"[ExcelConfigTool] Migrated {settings.entries.Count} URL entries " +
                    $"from EditorPrefs to {SETTINGS_PATH}."
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"[ExcelConfigTool] Cannot migrate legacy URL entries:\n{e}");
            }
        }
    }
}