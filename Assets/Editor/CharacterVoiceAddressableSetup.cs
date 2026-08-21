#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

/// <summary>
/// Đăng ký các clip voice của hero (CharacterVoice/skilltalk_english|vietnamese) vào Addressables
/// với address dạng: CharacterVoice/english/{clipName}, CharacterVoice/vietnamese/{clipName}.
/// Chạy: Tools/Game Data/Register Character Voice
///
/// Runtime build address tương tự trong HeroActor (GetVoiceFolder) nên clip phải được đăng ký
/// bằng đúng 2 thư mục en/vi này thì mới load được.
/// </summary>
public static class CharacterVoiceAddressableSetup
{
    private const string VoiceGroupName = "CharacterVoice";
    private const string BaseVoiceFolder = "Assets/Immortal Switch/CharacterVoice";

    private static readonly Dictionary<string, string> LanguageFolders = new()
    {
        ["english"] = "english",
        ["vietnamese"] = "vietnamese",
    };

    [MenuItem("Tools/Game Data/Register Character Voice")]
    public static void RegisterAll()
    {
        AddressableAssetSettings settings = GetOrCreateSettings();
        if (settings == null)
            return;

        AddressableAssetGroup group = GetOrCreateGroup(settings);

        int added = 0;
        int skipped = 0;

        foreach (KeyValuePair<string, string> pair in LanguageFolders)
        {
            string languageKey = pair.Key;
            string folderRelative = pair.Value;

            string sourceFolder = $"{BaseVoiceFolder}/skilltalk_{folderRelative}";
            if (!Directory.Exists(sourceFolder))
            {
                Debug.LogWarning($"[CharacterVoice] Thư mục không tồn tại: {sourceFolder}");
                continue;
            }

            string[] oggFiles = Directory.GetFiles(sourceFolder, "*.ogg", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < oggFiles.Length; i++)
            {
                string assetPath = oggFiles[i].Replace('\\', '/');
                string clipName = Path.GetFileNameWithoutExtension(assetPath);
                string address = $"CharacterVoice/{languageKey}/{clipName}";

                if (TrySetAddress(settings, group, assetPath, address))
                    added++;
                else
                    skipped++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[CharacterVoice] Đăng ký xong. Added/Updated: {added}, Skipped: {skipped}. Group: {VoiceGroupName}");
    }

    private static AddressableAssetSettings GetOrCreateSettings()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

        if (settings == null)
        {
            // Tự tạo default settings nếu chưa tồn tại (tương tự khi mở Addressables Groups lần đầu).
            settings = AddressableAssetSettings.Create(
                "Assets/AddressableAssetsData",
                "AddressableAssetSettings",
                createDefaultGroups: true,
                isPersisted: true);

            if (settings != null)
                AddressableAssetSettingsDefaultObject.Settings = settings;
        }

        if (settings == null)
        {
            Debug.LogError("[CharacterVoice] Không tạo được AddressableAssetSettings. Hãy mở Addressables Groups trước.");
            return null;
        }

        return settings;
    }

    private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings)
    {
        AddressableAssetGroup group = settings.FindGroup(VoiceGroupName);

        if (group != null)
            return group;

        group = settings.CreateGroup(
            VoiceGroupName,
            setAsDefaultGroup: false,
            readOnly: false,
            postEvent: true,
            schemasToCopy: null,
            typeof(BundledAssetGroupSchema),
            typeof(ContentUpdateGroupSchema)
        );

        return group;
    }

    private static bool TrySetAddress(
        AddressableAssetSettings settings,
        AddressableAssetGroup group,
        string assetPath,
        string address)
    {
        if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(address))
            return false;

        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogWarning($"[CharacterVoice] Không lấy được GUID của: {assetPath}");
            return false;
        }

        // Nếu asset đã thuộc group tại cùng address, không cần làm lại; address trùng là OK đối với Addressables.
        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: true);

        if (entry == null)
        {
            Debug.LogWarning($"[CharacterVoice] Không tạo được entry cho: {assetPath}");
            return false;
        }

        if (entry.address != address)
        {
            entry.SetAddress(address);
        }

        return true;
    }
}

#endif