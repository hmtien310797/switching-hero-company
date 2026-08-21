using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwitchingHero.BuildTool
{
    /// <summary>
    /// Tool build tùy chọn: chọn Active Config (Dev/License/Production), target APK/AAB,
    /// Addressable Local/Remote, cấu hình group remote/local rồi bấm 1 nút Build.
    /// </summary>
    public class SwitchingHeroBuildToolWindow : EditorWindow
    {
        private const string SettingsAssetPath = "Assets/EditorBuildTool/BuildToolSettings.asset";
        private const string LoginScenePath = "Assets/Scenes/LoginScene.unity";

        private enum ServerEnv { Dev, License, Production }
        private enum BuildTargetKind { Apk, Aab }

        // Nguồn group remote/local cho build REMOTE — gom từ AddressableAssetSettings.
        private enum RemoteGroupSource { Auto, FromSettings }

        // --- Persisted ---
        private BuildToolSettings _settings;

        // --- State cửa sổ ---
        private ServerEnv _env = ServerEnv.Dev;
        private BuildTargetKind _target = BuildTargetKind.Apk;
        private bool _addressableRemote = true;
        private Vector2 _scroll;

        // --- Options (const) ---
        private const string RemoteProfileName = "8.232.94.34:80";
        private const string LocalProfileName = "Local";
        private const string ProfileVarLocalBuild = "Local.BuildPath";
        private const string ProfileVarLocalLoad = "Local.LoadPath";
        private const string ProfileVarRemoteBuild = "Remote.BuildPath";
        private const string ProfileVarRemoteLoad = "Remote.LoadPath";

        // Baseline mặc định: group thường muốn patch (data/prefab/spine/audio/locale tables).
        private static readonly string[] BaselineRemoteGroups =
        {
            "BossData", "BossPrefab", "CreepData", "CreepPrefab", "HeroData", "HeroPrefab",
            "HeroSpine", "SkillData", "Skill", "GameDataBase", "Map", "Projectile", "SFX",
            "ui_atlas",
            "Localization-String-Tables-Chinese (Simplified) (zh-Hans)",
            "Localization-String-Tables-Chinese (Traditional) (zh-Hant)",
            "Localization-String-Tables-English (en)",
            "Localization-String-Tables-Vietnamese (vi)",
        };

        [MenuItem("Tools/Switching Hero/Build Tool")]
        private static void Open()
        {
            GetWindow<SwitchingHeroBuildToolWindow>("Build Tool");
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        // ── Settings SO ──────────────────────────────────────────────────────

        private void LoadSettings()
        {
            _settings = AssetDatabase.LoadAssetAtPath<BuildToolSettings>(SettingsAssetPath);
            if (_settings != null) return;

            EnsureFolderForAsset(SettingsAssetPath);
            _settings = CreateInstance<BuildToolSettings>();
            PreTickBaseline(_settings);
            AssetDatabase.CreateAsset(_settings, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildTool] Created {SettingsAssetPath} with default remote-group mapping.");
        }

        private static void PreTickBaseline(BuildToolSettings so)
        {
            var remote = new HashSet<string>(BaselineRemoteGroups);
            foreach (var name in GetAllGroupNames())
                so.groupRemoting.Add(new BuildToolSettings.GroupRemoting(name, remote.Contains(name)));
        }

        private void EnsureFolderForAsset(string assetPath)
        {
            var folder = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder)) return;

            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            var leaf = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent))
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(parent));
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // ── UI ────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (_settings == null) LoadSettings();

            EditorGUILayout.Space();
            DrawBuildConfig();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            DrawGroupConfig();
        }

        private void DrawBuildConfig()
        {
            EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);

            _env = (ServerEnv)EditorGUILayout.EnumPopup("Active Config", _env);
            _target = (BuildTargetKind)EditorGUILayout.EnumPopup("Build Target", _target);
            _addressableRemote = EditorGUILayout.Toggle("Addressable Remote", _addressableRemote);
            _settings.outputPath = EditorGUILayout.TextField("Output Path", _settings.outputPath);

            EditorGUILayout.Space();
            EditorGUI.BeginDisabledGroup(_settings == null);
            if (GUILayout.Button("Build", GUILayout.Height(32)))
                DoBuild();
            EditorGUI.EndDisabledGroup();

            if (_addressableRemote)
            {
                EditorGUILayout.HelpBox(
                    "Remote: build theo mapping group bên dưới (nhóm tick Remote sẽ lên CDN, còn lại local nhúng APK).",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Local: MỌI group về local (nhúng APK), NoHash, Prevent Update = false. Group-rating bên dưới bỏ qua.",
                    MessageType.Info);
            }
        }

        private void DrawGroupConfig()
        {
            EditorGUILayout.LabelField("Addressable Group Config (for REMOTE build)", EditorStyles.boldLabel);

            if (GUILayout.Button("Reload Groups from Addressables"))
                AddMissingGroups();

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(260));
            for (var i = 0; i < _settings.groupRemoting.Count; i++)
            {
                var entry = _settings.groupRemoting[i];
                var next = EditorGUILayout.ToggleLeft(entry.groupName, entry.remote);
                if (next != entry.remote)
                {
                    entry.remote = next;
                    EditorUtility.SetDirty(_settings);
                }
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Save Group Config"))
            {
                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
                Debug.Log("[BuildTool] Group remote mapping saved.");
            }
        }

        private void AddMissingGroups()
        {
            var settings = GetAddressableSettings();
            if (settings == null) return;

            var known = new HashSet<string>(_settings.groupRemoting.Select(x => x.groupName));
            var changed = false;
            foreach (var name in settings.groups.Select(g => g?.Name).Where(n => !string.IsNullOrEmpty(n)))
            {
                if (known.Contains(name)) continue;
                _settings.groupRemoting.Add(new BuildToolSettings.GroupRemoting(name, false));
                changed = true;
            }

            if (changed) EditorUtility.SetDirty(_settings);
            // Giữ thứ tự hiển thị khớp với danh sách group trong Addressables.
            SortGroupConfig();
        }

        private void SortGroupConfig()
        {
            var order = GetAllGroupNames().ToList();
            var index = new Dictionary<string, int>();
            for (var i = 0; i < order.Count; i++) index[order[i]] = i;

            _settings.groupRemoting.Sort((a, b) =>
            {
                index.TryGetValue(a.groupName, out var ai);
                index.TryGetValue(b.groupName, out var bi);
                int n = string.Compare(a.groupName, b.groupName, StringComparison.Ordinal);
                if (ai != bi) return ai - bi;
                return n;
            });
            EditorUtility.SetDirty(_settings);
        }

        // ── Build ─────────────────────────────────────────────────────────────

        private void DoBuild()
        {
            if (_settings == null) { Debug.LogError("[BuildTool] Settings missing."); return; }

            EditorUtility.DisplayProgressBar("Build Tool", "Setting Active Config...", 0.02f);
            if (!ApplyActiveConfig())
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            EditorUtility.DisplayProgressBar("Build Tool", "Configuring Addressables...", 0.06f);
            if (!ApplyAddressableMode(_addressableRemote))
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Build Tool", "Building Addressables...", 0.1f);
                AddressableAssetSettings.BuildPlayerContent();

                EditorUtility.DisplayProgressBar("Build Tool", "Building Player (" + _target + ")...", 0.7f);
                var report = BuildPlayer();

                if (report.summary.result == BuildResult.Succeeded)
                {
                    Debug.Log($"[BuildTool] BUILD OK → {report.summary.outputPath}");
                    EditorUtility.RevealInFinder(report.summary.outputPath);
                }
                else
                {
                    Debug.LogError($"[BuildTool] Build FAILED: {report.summary.result} — {report.summary.totalErrors} error(s)");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[BuildTool] Exception during build: {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // 1) Gán activeConfig trên NakamaClient trong LoginScene theo env chọn.
        private bool ApplyActiveConfig()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<NakamaServerConfig>(
                $"Assets/Resources/ServerConfig/{_env}.asset");
            if (cfg == null)
            {
                Debug.LogError($"[BuildTool] Không tìm thấy server config asset cho env {_env}.");
                return false;
            }

            SceneLoginOpen(scene =>
            {
                var nc = FindNakamaClientInScene(scene);
                if (nc == null)
                {
                    Debug.LogWarning("[BuildTool] Không tìm thấy component NakamaClient trong LoginScene.");
                    return;
                }

                var so = new SerializedObject(nc);
                var prop = so.FindProperty("activeConfig");
                if (prop == null)
                {
                    Debug.LogError("[BuildTool] Không tìm thấy serialized field 'activeConfig' trên NakamaClient.");
                    return;
                }

                prop.objectReferenceValue = cfg;
                so.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[BuildTool] Set NakamaClient activeConfig → {_env} và đã lưu LoginScene.");
            });
            return true;
        }

        private void SceneLoginOpen(Action<Scene> action)
        {
            Scene target = default;
            var alreadyLoaded = false;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.path == LoginScenePath) { target = s; alreadyLoaded = true; break; }
            }

            if (!alreadyLoaded)
                target = EditorSceneManager.OpenScene(LoginScenePath, OpenSceneMode.Additive);

            try { action(target); }
            finally
            {
                if (!alreadyLoaded)
                    EditorSceneManager.CloseScene(target, true);
            }
        }

        private static NakamaClient FindNakamaClientInScene(Scene scene)
        {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null || go.scene != scene) continue;
                var nc = go.GetComponent<NakamaClient>();
                if (nc != null) return nc;
            }
            return null;
        }

        // 2) Cấu hình Addressable theo mode (remote/local), ghi thẳng vào settings.
        private bool ApplyAddressableMode(bool remote)
        {
            var settings = GetAddressableSettings();
            if (settings == null) return false;

            var profileName = remote ? RemoteProfileName : LocalProfileName;
            var profileId = settings.profileSettings.GetProfileId(profileName);
            if (string.IsNullOrEmpty(profileId))
            {
                Debug.LogError($"[BuildTool] Không có Addressable profile \"{profileName}\". Hãy tạo profile này.");
                return false;
            }

            settings.activeProfileId = profileId;
            settings.BuildRemoteCatalog = remote;

            foreach (var group in settings.groups)
            {
                if (group == null) continue;
                var isRemoteGroup = remote && IsMarkedRemote(group.Name);

                var bundle = group.GetSchema<BundledAssetGroupSchema>();
                if (bundle != null)
                {
                    var buildVar = isRemoteGroup ? ProfileVarRemoteBuild : ProfileVarLocalBuild;
                    var loadVar = isRemoteGroup ? ProfileVarRemoteLoad : ProfileVarLocalLoad;
                    bundle.BuildPath.SetVariableByName(settings, buildVar);
                    bundle.LoadPath.SetVariableByName(settings, loadVar);
                    bundle.BundleNaming = isRemoteGroup
                        ? BundledAssetGroupSchema.BundleNamingStyle.AppendHash
                        : BundledAssetGroupSchema.BundleNamingStyle.NoHash;
                }

                var content = group.GetSchema<ContentUpdateGroupSchema>();
                if (content != null)
                {
                    // StaticContent = "Prevent Update". Remote group → true (patch được qua Content Update);
                    // local group → false.
                    content.StaticContent = isRemoteGroup;
                }
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildTool] Addressables → {(remote ? "REMOTE" : "LOCAL")} (profile \"{profileName}\", BuildRemoteCatalog={remote}).");
            return true;
        }

        private bool IsMarkedRemote(string groupName)
        {
            var entry = _settings != null
                ? _settings.groupRemoting.FirstOrDefault(x => x.groupName == groupName)
                : null;
            return entry != null && entry.remote;
        }

        // 3) Build player APK/AAB.
        private BuildReport BuildPlayer()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.buildAppBundle = (_target == BuildTargetKind.Aab);

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            var dir = _settings.outputPath;
            if (!dir.EndsWith("/") && !dir.EndsWith("\\")) dir += "/";
            Directory.CreateDirectory(dir);

            var ext = _target == BuildTargetKind.Aab ? "aab" : "apk";
            var fileName = $"{PlayerSettings.productName}_{_env}_{(_addressableRemote ? "remote" : "local")}.{ext}";
            var output = Path.Combine(dir, fileName);

            return BuildPipeline.BuildPlayer(scenes, output, BuildTarget.Android, BuildOptions.None);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static AddressableAssetSettings GetAddressableSettings()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
                Debug.LogError("[BuildTool] Không mở được AddressableAssetSettings.");
            return settings;
        }

        private static IEnumerable<string> GetAllGroupNames()
        {
            var settings = GetAddressableSettings();
            return settings == null
                ? Enumerable.Empty<string>()
                : settings.groups.Select(g => g?.Name).Where(n => !string.IsNullOrEmpty(n));
        }
    }
}