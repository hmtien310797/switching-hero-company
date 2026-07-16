using System;
using System.Collections.Generic;
using System.IO;
using Editor.LocalizationSyncTool.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Editor.LocalizationSyncTool.Services
{
    public static class LocalizationSyncToolSettingsStore
    {
        public const string SETTINGS_PATH = "ProjectSettings/LocalizationSyncSettings.json";

        public static LocalizationSyncToolSettings Load()
        {
            LocalizationSyncToolSettings settings = null;

            if (File.Exists(SETTINGS_PATH))
            {
                try
                {
                    settings = JsonConvert.DeserializeObject<LocalizationSyncToolSettings>(
                        File.ReadAllText(SETTINGS_PATH)
                    );
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[LocalizationSync] Không thể đọc settings:\n{exception}");
                }
            }

            settings ??= new LocalizationSyncToolSettings();
            settings.entries ??= new List<LocalizationSyncUrlEntry>();
            settings.localeMappings ??= new List<LocalizationSyncLocaleMapping>();

            if (!File.Exists(SETTINGS_PATH))
            {
                Save(settings);
            }

            return settings;
        }

        public static void Save(LocalizationSyncToolSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.entries ??= new List<LocalizationSyncUrlEntry>();
            settings.localeMappings ??= new List<LocalizationSyncLocaleMapping>();
            Directory.CreateDirectory(Path.GetDirectoryName(SETTINGS_PATH) ?? "ProjectSettings");

            File.WriteAllText(
                SETTINGS_PATH,
                JsonConvert.SerializeObject(settings, Formatting.Indented)
            );
        }
    }
}