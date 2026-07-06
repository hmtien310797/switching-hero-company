#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pooling.Editor
{
    [CustomEditor(typeof(AddressablePoolConfigSO))]
    public sealed class AddressablePoolConfigEditor : UnityEditor.Editor
    {
        private const string SkillGroupName = "Skill";
        private const int DefaultWarmupCount = 10;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(12f);

            EditorGUILayout.HelpBox(
                $"Lấy toàn bộ Addressable asset trong group " +
                $"'{SkillGroupName}' và tạo pool entry với Warmup Count = " +
                $"{DefaultWarmupCount}.",
                MessageType.Info
            );

            if (GUILayout.Button(
                    "Generate Pool Entries From Skill Group",
                    GUILayout.Height(32f)))
            {
                AddressablePoolConfigSO config =
                    (AddressablePoolConfigSO)target;

                GenerateEntries(config);
            }
        }

        private static void GenerateEntries(
            AddressablePoolConfigSO config)
        {
            if (config == null)
            {
                Debug.LogError(
                    "[AddressablePoolConfigEditor] Config is null."
                );

                return;
            }

            AddressableAssetSettings settings =
                AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                Debug.LogError(
                    "[AddressablePoolConfigEditor] " +
                    "AddressableAssetSettings not found."
                );

                return;
            }

            AddressableAssetGroup skillGroup =
                settings.FindGroup(SkillGroupName);

            if (skillGroup == null)
            {
                Debug.LogError(
                    $"[AddressablePoolConfigEditor] " +
                    $"Addressable group '{SkillGroupName}' not found."
                );

                return;
            }

            var addressableEntries =
                new List<AddressableAssetEntry>();

            /*
             * includeSelf: true
             * recurseAll: true
             * includeSubObjects: false
             *
             * recurseAll giúp lấy cả asset nằm trong Addressable folder entry.
             */
            skillGroup.GatherAllAssets(
                addressableEntries,
                includeSelf: true,
                recurseAll: true,
                includeSubObjects: false
            );

            var uniqueKeys =
                new HashSet<string>();

            var poolEntries =
                new List<AddressablePoolEntry>(
                    addressableEntries.Count
                );

            for (int i = 0;
                 i < addressableEntries.Count;
                 i++)
            {
                AddressableAssetEntry addressableEntry =
                    addressableEntries[i];

                if (addressableEntry == null)
                    continue;

                string address =
                    addressableEntry.address;

                if (string.IsNullOrWhiteSpace(address))
                    continue;

                address = address.Trim();

                // Tránh entry bị trùng key.
                if (!uniqueKeys.Add(address))
                    continue;

                poolEntries.Add(
                    new AddressablePoolEntry
                    {
                        key = address,
                        warmupCount = DefaultWarmupCount,
                        parent = null
                    }
                );
            }

            // Sắp xếp theo key để dễ kiểm tra trong Inspector.
            poolEntries.Sort(
                static (a, b) =>
                    string.CompareOrdinal(a.key, b.key)
            );

            Undo.RecordObject(
                config,
                "Generate Skill Addressable Pool Entries"
            );

            config.entries =
                poolEntries.ToArray();

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[AddressablePoolConfigEditor] Generated " +
                $"{poolEntries.Count} pool entries from group " +
                $"'{SkillGroupName}'. WarmupCount=" +
                $"{DefaultWarmupCount}.",
                config
            );
        }
    }
}

#endif