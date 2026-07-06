using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace SwitchingHero.Editor
{
    // Google Sign-In needs its REVERSED_CLIENT_ID (from GoogleService-Info.plist)
    // registered as a URL scheme so iOS can route the sign-in redirect back to
    // the app. Unity's Xcode export doesn't carry this over on its own.
    public class GoogleSignInUrlSchemePostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
#if UNITY_IOS
            if (report.summary.platform != BuildTarget.iOS)
                return;

            var sourcePlistPath = Path.Combine(Application.dataPath, "Plugins/iOS/GoogleService-Info.plist");
            if (!File.Exists(sourcePlistPath))
            {
                Debug.LogWarning("[GoogleSignInUrlSchemePostprocessor] GoogleService-Info.plist not found, skipping Google Sign-In setup.");
                return;
            }

            // Unity copies the plist onto disk under Libraries/Plugins/iOS (same
            // as the GoogleSignIn .h/.mm files), but unlike recognized native
            // plugin types it does NOT add it to the Xcode "Copy Bundle
            // Resources" build phase, so it never lands inside the .app and
            // pathForResource:ofType: returns nil at runtime. Add it explicitly.
            const string relativePlistPath = "Libraries/Plugins/iOS/GoogleService-Info.plist";
            var copiedPlistPath = Path.Combine(report.summary.outputPath, relativePlistPath);
            Directory.CreateDirectory(Path.GetDirectoryName(copiedPlistPath));
            File.Copy(sourcePlistPath, copiedPlistPath, true);

            var projectPath = PBXProject.GetPBXProjectPath(report.summary.outputPath);
            var pbxProject = new PBXProject();
            pbxProject.ReadFromString(File.ReadAllText(projectPath));
            var targetGuid = pbxProject.GetUnityMainTargetGuid();
            var fileGuid = pbxProject.AddFile(relativePlistPath, relativePlistPath, PBXSourceTree.Source);
            pbxProject.AddFileToBuild(targetGuid, fileGuid);
            pbxProject.WriteToFile(projectPath);

            var sourcePlist = new PlistDocument();
            sourcePlist.ReadFromFile(sourcePlistPath);
            if (!sourcePlist.root.values.TryGetValue("REVERSED_CLIENT_ID", out var reversedClientIdElement) ||
                !(reversedClientIdElement is PlistElementString reversedClientIdString))
            {
                Debug.LogWarning("[GoogleSignInUrlSchemePostprocessor] REVERSED_CLIENT_ID key missing from GoogleService-Info.plist, skipping URL scheme registration.");
                return;
            }
            var reversedClientId = reversedClientIdString.value;

            var infoPlistPath = Path.Combine(report.summary.outputPath, "Info.plist");
            var infoPlist = new PlistDocument();
            infoPlist.ReadFromFile(infoPlistPath);

            PlistElementArray urlTypes;
            if (infoPlist.root.values.TryGetValue("CFBundleURLTypes", out var existing) &&
                existing is PlistElementArray existingArray)
            {
                urlTypes = existingArray;
            }
            else
            {
                urlTypes = infoPlist.root.CreateArray("CFBundleURLTypes");
            }

            var urlTypeDict = urlTypes.AddDict();
            urlTypeDict.SetString("CFBundleURLName", "google-signin");
            urlTypeDict.CreateArray("CFBundleURLSchemes").AddString(reversedClientId);

            infoPlist.WriteToFile(infoPlistPath);
#endif
        }
    }
}
