#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Immortal_Switch.EditorTools
{
    public sealed class AddressablesDebugWindow : EditorWindow
    {
        private const string RequiredMarkerKey = "addr_content_ready_Required";
        private const string DefaultRequiredLabel = "Required";
        private const string DefaultPreloadLabel = "Preload";

        private string _requiredLabel = DefaultRequiredLabel;
        private string _preloadLabel = DefaultPreloadLabel;
        private string _customLabel = string.Empty;

        private bool _isRunning;
        private float _progress;
        private string _status = "Ready";
        private Vector2 _scrollPosition;
        private readonly List<string> _logs = new();

        [MenuItem("Tools/Addressables Debug Tool")]
        private static void OpenWindow()
        {
            var window = GetWindow<AddressablesDebugWindow>();
            window.titleContent = new GUIContent("Addressables Debug");
            window.minSize = new Vector2(520f, 560f);
            window.Show();
        }

        [MenuItem("Tools/Addressables Debug/Clear All Bundle Cache")]
        private static void ClearAllCacheFromMenu()
        {
            bool cleared = Caching.ClearCache();

            if (cleared)
            {
                PlayerPrefs.DeleteKey(RequiredMarkerKey);
                PlayerPrefs.Save();
                Debug.Log("[AddressablesDebug] Cleared all Unity AssetBundle cache.");
            }
            else
            {
                Debug.LogWarning(
                    "[AddressablesDebug] Could not clear cache. " +
                    "A bundle may still be loaded.");
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(
                "Addressables Remote Debug Tool",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Dùng cùng Play Mode Script = Use Existing Build để test đúng bundle remote. " +
                "Các thao tác Addressables nên chạy trong Play Mode.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(_isRunning))
            {
                DrawCacheSection();
                EditorGUILayout.Space(8);
                DrawCatalogSection();
                EditorGUILayout.Space(8);
                DrawDownloadSection();
            }

            EditorGUILayout.Space(12);
            DrawProgress();
            EditorGUILayout.Space(8);
            DrawLogs();
        }

        private void DrawCacheSection()
        {
            EditorGUILayout.LabelField("Cache", EditorStyles.boldLabel);

            if (GUILayout.Button("Clear All Unity AssetBundle Cache", GUILayout.Height(30)))
            {
                ClearAllCacheAsync();
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear Required Cache"))
            {
                ClearLabelCacheAsync(_requiredLabel);
            }

            if (GUILayout.Button("Clear Preload Cache"))
            {
                ClearLabelCacheAsync(_preloadLabel);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Clean Unused Bundles From Current Catalog"))
            {
                CleanUnusedBundlesAsync();
            }

            if (GUILayout.Button("Clear Required Ready Marker"))
            {
                PlayerPrefs.DeleteKey(RequiredMarkerKey);
                PlayerPrefs.Save();
                AddLog($"Deleted PlayerPrefs marker: {RequiredMarkerKey}");
            }
        }

        private void DrawCatalogSection()
        {
            EditorGUILayout.LabelField("Remote Catalog", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Check Catalog Updates"))
            {
                CheckCatalogUpdatesAsync(updateCatalogs: false);
            }

            if (GUILayout.Button("Check + Update Catalogs"))
            {
                CheckCatalogUpdatesAsync(updateCatalogs: true);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDownloadSection()
        {
            EditorGUILayout.LabelField("Labels", EditorStyles.boldLabel);

            _requiredLabel = EditorGUILayout.TextField(
                "Required Label",
                _requiredLabel);

            _preloadLabel = EditorGUILayout.TextField(
                "Localization Label",
                _preloadLabel);

            _customLabel = EditorGUILayout.TextField(
                "Custom Label",
                _customLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Print Required Size"))
            {
                PrintLabelSizeAsync(_requiredLabel);
            }

            if (GUILayout.Button("Download Required"))
            {
                DownloadLabelAsync(_requiredLabel);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Print Preload Size"))
            {
                PrintLabelSizeAsync(_preloadLabel);
            }

            if (GUILayout.Button("Download Preload"))
            {
                DownloadLabelAsync(_preloadLabel);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Print Custom Size"))
            {
                PrintLabelSizeAsync(_customLabel);
            }

            if (GUILayout.Button("Download Custom"))
            {
                DownloadLabelAsync(_customLabel);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Clear Custom Label Cache"))
            {
                ClearLabelCacheAsync(_customLabel);
            }
        }

        private void DrawProgress()
        {
            EditorGUILayout.LabelField("Operation", EditorStyles.boldLabel);

            Rect rect = EditorGUILayout.GetControlRect(false, 22f);
            EditorGUI.ProgressBar(
                rect,
                Mathf.Clamp01(_progress),
                $"{_status} ({_progress * 100f:F0}%)");

            if (_isRunning)
            {
                Repaint();
            }
        }

        private void DrawLogs()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Logs", EditorStyles.boldLabel);

            if (GUILayout.Button("Clear Logs", GUILayout.Width(90)))
            {
                _logs.Clear();
            }

            EditorGUILayout.EndHorizontal();

            _scrollPosition = EditorGUILayout.BeginScrollView(
                _scrollPosition,
                GUILayout.ExpandHeight(true));

            foreach (string log in _logs)
            {
                EditorGUILayout.SelectableLabel(
                    log,
                    EditorStyles.textArea,
                    GUILayout.MinHeight(34f));
            }

            EditorGUILayout.EndScrollView();
        }

        private async void ClearAllCacheAsync()
        {
            if (!TryBeginOperation("Clearing all cache"))
                return;

            try
            {
                await Resources.UnloadUnusedAssets();

                bool cleared = Caching.ClearCache();

                if (!cleared)
                {
                    throw new InvalidOperationException(
                        "Caching.ClearCache returned false. " +
                        "Some AssetBundles may still be loaded.");
                }

                PlayerPrefs.DeleteKey(RequiredMarkerKey);
                PlayerPrefs.Save();

                CompleteOperation("All AssetBundle cache cleared");
            }
            catch (Exception exception)
            {
                FailOperation(exception);
            }
        }

        private async void ClearLabelCacheAsync(string label)
        {
            if (!ValidateLabel(label))
                return;

            if (!TryBeginOperation($"Clearing cache: {label}"))
                return;

            AsyncOperationHandle<bool> handle = default;

            try
            {
                handle = Addressables.ClearDependencyCacheAsync(
                    (object)label,
                    false);

                bool result = await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded || !result)
                {
                    throw handle.OperationException ??
                          new InvalidOperationException(
                              $"Could not clear dependency cache for '{label}'.");
                }

                if (string.Equals(
                        label,
                        DefaultRequiredLabel,
                        StringComparison.Ordinal))
                {
                    PlayerPrefs.DeleteKey(RequiredMarkerKey);
                    PlayerPrefs.Save();
                }

                CompleteOperation($"Cache cleared: {label}");
            }
            catch (Exception exception)
            {
                FailOperation(exception);
            }
            finally
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }

        private async void CleanUnusedBundlesAsync()
        {
            if (!TryBeginOperation("Cleaning unused bundles"))
                return;

            AsyncOperationHandle<bool> handle = default;

            try
            {
                handle = Addressables.CleanBundleCache();
                bool result = await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded || !result)
                {
                    throw handle.OperationException ??
                          new InvalidOperationException(
                              "Addressables.CleanBundleCache failed.");
                }

                CompleteOperation("Unused bundles cleaned");
            }
            catch (Exception exception)
            {
                FailOperation(exception);
            }
            finally
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }

        private async void CheckCatalogUpdatesAsync(bool updateCatalogs)
        {
            if (!TryBeginOperation(
                    updateCatalogs
                        ? "Checking and updating catalogs"
                        : "Checking catalog updates"))
            {
                return;
            }

            AsyncOperationHandle<List<string>> checkHandle = default;
            AsyncOperationHandle<List<UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator>>
                updateHandle = default;

            try
            {
                _progress = 0.1f;
                checkHandle = Addressables.CheckForCatalogUpdates(false);
                List<string> catalogs = await checkHandle.Task;

                if (checkHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw checkHandle.OperationException ??
                          new InvalidOperationException(
                              "CheckForCatalogUpdates failed.");
                }

                if (catalogs == null || catalogs.Count == 0)
                {
                    CompleteOperation("No catalog update found");
                    return;
                }

                AddLog($"Found {catalogs.Count} catalog update(s):");
                foreach (string catalog in catalogs)
                {
                    AddLog(catalog);
                }

                if (!updateCatalogs)
                {
                    CompleteOperation(
                        $"Found {catalogs.Count} catalog update(s)");
                    return;
                }

                _progress = 0.5f;
                _status = "Updating catalogs";

                updateHandle = Addressables.UpdateCatalogs(
                    catalogs,
                    false);

                await updateHandle.Task;

                if (updateHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw updateHandle.OperationException ??
                          new InvalidOperationException(
                              "UpdateCatalogs failed.");
                }

                CompleteOperation(
                    $"Updated {catalogs.Count} catalog(s)");
            }
            catch (Exception exception)
            {
                FailOperation(exception);
            }
            finally
            {
                if (updateHandle.IsValid())
                {
                    Addressables.Release(updateHandle);
                }

                if (checkHandle.IsValid())
                {
                    Addressables.Release(checkHandle);
                }
            }
        }

        private async void PrintLabelSizeAsync(string label)
        {
            if (!ValidateLabel(label))
                return;

            if (!TryBeginOperation($"Calculating size: {label}"))
                return;

            AsyncOperationHandle<IList<IResourceLocation>> locationsHandle =
                default;

            AsyncOperationHandle<long> sizeHandle = default;

            try
            {
                locationsHandle = Addressables.LoadResourceLocationsAsync(
                    (object)label,
                    null);

                IList<IResourceLocation> locations =
                    await locationsHandle.Task;

                if (locationsHandle.Status != AsyncOperationStatus.Succeeded ||
                    locations == null ||
                    locations.Count == 0)
                {
                    throw locationsHandle.OperationException ??
                          new InvalidOperationException(
                              $"No locations found for label '{label}'.");
                }

                sizeHandle = Addressables.GetDownloadSizeAsync(locations);
                long bytes = await sizeHandle.Task;

                if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw sizeHandle.OperationException ??
                          new InvalidOperationException(
                              $"Could not calculate size for '{label}'.");
                }

                CompleteOperation(
                    $"{label}: {FormatBytes(bytes)} remaining");
            }
            catch (Exception exception)
            {
                FailOperation(exception);
            }
            finally
            {
                if (sizeHandle.IsValid())
                {
                    Addressables.Release(sizeHandle);
                }

                if (locationsHandle.IsValid())
                {
                    Addressables.Release(locationsHandle);
                }
            }
        }

        private async void DownloadLabelAsync(string label)
        {
            if (!ValidateLabel(label))
                return;

            if (!TryBeginOperation($"Downloading label: {label}"))
                return;

            AsyncOperationHandle<IList<IResourceLocation>> locationsHandle =
                default;

            AsyncOperationHandle<long> sizeHandle = default;
            AsyncOperationHandle downloadHandle = default;

            try
            {
                locationsHandle = Addressables.LoadResourceLocationsAsync(
                    (object)label,
                    null);

                IList<IResourceLocation> locations =
                    await locationsHandle.Task;

                if (locationsHandle.Status != AsyncOperationStatus.Succeeded ||
                    locations == null ||
                    locations.Count == 0)
                {
                    throw locationsHandle.OperationException ??
                          new InvalidOperationException(
                              $"No locations found for label '{label}'.");
                }

                sizeHandle = Addressables.GetDownloadSizeAsync(locations);
                long expectedBytes = await sizeHandle.Task;

                if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw sizeHandle.OperationException ??
                          new InvalidOperationException(
                              $"Could not calculate size for '{label}'.");
                }

                if (expectedBytes <= 0)
                {
                    CompleteOperation(
                        $"{label} is already cached");
                    return;
                }

                AddLog(
                    $"Downloading {label}: {FormatBytes(expectedBytes)}");

                downloadHandle = Addressables.DownloadDependenciesAsync(
                    locations,
                    false);

                while (!downloadHandle.IsDone)
                {
                    var status = downloadHandle.GetDownloadStatus();

                    _progress = status.TotalBytes > 0
                        ? Mathf.Clamp01(
                            status.DownloadedBytes /
                            (float)status.TotalBytes)
                        : downloadHandle.PercentComplete;

                    _status =
                        $"Downloading {label}: " +
                        $"{FormatBytes(status.DownloadedBytes)} / " +
                        $"{FormatBytes(status.TotalBytes)}";

                    Repaint();
                    await Task.Yield();
                }

                await downloadHandle.Task;

                if (downloadHandle.Status !=
                    AsyncOperationStatus.Succeeded)
                {
                    throw downloadHandle.OperationException ??
                          new InvalidOperationException(
                              $"Download failed for '{label}'.");
                }

                CompleteOperation($"Downloaded: {label}");
            }
            catch (Exception exception)
            {
                FailOperation(exception);
            }
            finally
            {
                if (downloadHandle.IsValid())
                {
                    Addressables.Release(downloadHandle);
                }

                if (sizeHandle.IsValid())
                {
                    Addressables.Release(sizeHandle);
                }

                if (locationsHandle.IsValid())
                {
                    Addressables.Release(locationsHandle);
                }
            }
        }

        private bool TryBeginOperation(string status)
        {
            if (_isRunning)
            {
                AddLog("Another operation is already running.");
                return false;
            }

            _isRunning = true;
            _progress = 0f;
            _status = status;
            AddLog(status);
            Repaint();
            return true;
        }

        private void CompleteOperation(string message)
        {
            _progress = 1f;
            _status = message;
            _isRunning = false;
            AddLog(message);
            Debug.Log($"[AddressablesDebug] {message}");
            Repaint();
        }

        private void FailOperation(Exception exception)
        {
            _progress = 0f;
            _status = "Failed";
            _isRunning = false;

            string message =
                $"{exception.GetType().Name}: {exception.Message}";

            AddLog(message);
            Debug.LogError(
                $"[AddressablesDebug] {exception}");
            Repaint();
        }

        private bool ValidateLabel(string label)
        {
            if (!string.IsNullOrWhiteSpace(label))
                return true;

            EditorUtility.DisplayDialog(
                "Addressables Debug",
                "Label không được để trống.",
                "OK");

            return false;
        }

        private void AddLog(string message)
        {
            _logs.Insert(
                0,
                $"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0)
                return "0 B";

            string[] suffixes =
            {
                "B",
                "KB",
                "MB",
                "GB"
            };

            int suffixIndex = 0;
            double value = bytes;

            while (value >= 1024d &&
                   suffixIndex < suffixes.Length - 1)
            {
                value /= 1024d;
                suffixIndex++;
            }

            return $"{value:0.##} {suffixes[suffixIndex]}";
        }
    }
}
#endif
