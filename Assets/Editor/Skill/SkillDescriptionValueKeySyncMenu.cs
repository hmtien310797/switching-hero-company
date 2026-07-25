using System.Text;
using Immortal_Switch.Scripts.Skill;
using UnityEditor;
using UnityEngine;

namespace Editor.Skill
{
    /// <summary>
    /// Batch sync <see cref="SkillDataSO.DescriptionValuesByLevel"/> cho toàn bộ
    /// Ultimate/Passive skill asset trong project, theo số placeholder của
    /// localized description template. Dùng Safe Sync (chỉ grow, không xóa data).
    /// Đọc String Table trực tiếp qua editor API, không cần Play Mode.
    /// </summary>
    public static class SkillDescriptionValueKeySyncMenu
    {
        private const string MenuPath = "Tools/Skill/Sync All Localized Description Values";

        [MenuItem(MenuPath)]
        public static void SyncAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:SkillDataSO");

            int scanned = 0;
            int specialSkills = 0;
            int updated = 0;
            int unchanged = 0;
            int missingKey = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    SkillDataSO asset = AssetDatabase.LoadAssetAtPath<SkillDataSO>(assetPath);

                    if (asset == null)
                        continue;

                    scanned++;

                    if (!asset.IsSpecialSkill())
                        continue;

                    specialSkills++;

                    SkillDataSO.SkillValueKeySyncStatus status =
                        asset.SyncDescriptionValuesWithLocalizedTemplate(false);

                    switch (status)
                    {
                        case SkillDataSO.SkillValueKeySyncStatus.Updated:
                            updated++;
                            EditorUtility.SetDirty(asset);
                            break;

                        case SkillDataSO.SkillValueKeySyncStatus.MissingLocalizationKey:
                            missingKey++;
                            break;

                        default:
                            unchanged++;
                            break;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var summary = new StringBuilder();
            summary.AppendLine("[SkillDescriptionValueKeySyncMenu] Sync finished.");
            summary.AppendLine($"Scanned: {scanned}");
            summary.AppendLine($"Special skills: {specialSkills}");
            summary.AppendLine($"Updated: {updated}");
            summary.AppendLine($"Unchanged: {unchanged}");
            summary.AppendLine($"Missing localization key: {missingKey}");

            Debug.Log(summary.ToString());
        }

        [MenuItem("Tools/Skill/Reinitialize All Description Values")]
        public static void ReinitializeAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:SkillDataSO");

            int scanned = 0;
            int specialSkills = 0;
            int updated = 0;
            int missingKey = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    SkillDataSO asset = AssetDatabase.LoadAssetAtPath<SkillDataSO>(assetPath);

                    if (asset == null)
                        continue;

                    scanned++;

                    if (!asset.IsSpecialSkill())
                        continue;

                    specialSkills++;

                    SkillDataSO.SkillValueKeySyncStatus status =
                        asset.ReinitializeDescriptionValues();

                    switch (status)
                    {
                        case SkillDataSO.SkillValueKeySyncStatus.Updated:
                            updated++;
                            EditorUtility.SetDirty(asset);
                            break;

                        case SkillDataSO.SkillValueKeySyncStatus.MissingLocalizationKey:
                            missingKey++;
                            break;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var summary = new StringBuilder();
            summary.AppendLine("[SkillDescriptionValueKeySyncMenu] Reinitialize finished.");
            summary.AppendLine($"Scanned: {scanned}");
            summary.AppendLine($"Special skills: {specialSkills}");
            summary.AppendLine($"Reinitialized: {updated}");
            summary.AppendLine($"Missing localization key: {missingKey}");

            Debug.Log(summary.ToString());
        }
    }
}
