using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace SwitchingHero.Editor
{
    // Apple requires NSUserTrackingUsageDescription in Info.plist for any app that can show
    // the App Tracking Transparency prompt (see AppTrackingTransparencyBridge.cs), otherwise
    // the app crashes when ATTrackingManager.requestTrackingAuthorization is called and App
    // Store review rejects the binary outright.
    public class AppTrackingTransparencyPostprocessor : IPostprocessBuildWithReport
    {
        // TODO: đã đúng với mục đích sử dụng dữ liệu tracking thực tế (quảng cáo cá nhân hoá,
        // đo lường hiệu quả quảng cáo, ...) trước khi tích hợp SDK quảng cáo/attribution đầu tiên.
        private const string UsageDescription =
            "Dữ liệu này giúp chúng tôi cá nhân hoá quảng cáo và đo lường hiệu quả quảng cáo phù hợp với bạn hơn.";

        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
#if UNITY_IOS
            if (report.summary.platform != BuildTarget.iOS)
                return;

            var infoPlistPath = Path.Combine(report.summary.outputPath, "Info.plist");
            var infoPlist = new PlistDocument();
            infoPlist.ReadFromFile(infoPlistPath);
            infoPlist.root.SetString("NSUserTrackingUsageDescription", UsageDescription);
            infoPlist.WriteToFile(infoPlistPath);

            var projectPath = PBXProject.GetPBXProjectPath(report.summary.outputPath);
            var pbxProject = new PBXProject();
            pbxProject.ReadFromString(File.ReadAllText(projectPath));

            // AppTrackingTransparency.mm (Assets/Plugins/iOS) is compiled into the
            // UnityFramework target, not the Unity-iPhone app target — since Unity 2019.3
            // native plugin code always builds there. Linking the framework only to the app
            // target leaves UnityFramework itself unresolved at link time, which is exactly
            // the "_OBJC_CLASS_$_ATTrackingManager" undefined symbol error.
            var frameworkTargetGuid = pbxProject.GetUnityFrameworkTargetGuid();
            var mainTargetGuid = pbxProject.GetUnityMainTargetGuid();

            // Weak-link: the framework only exists on iOS 14+, and the app targets iOS 13
            // (ProjectSettings iOSTargetOSVersionString), so it must not hard-fail loading
            // on older devices. AppTrackingTransparencyBridge already guards the API with
            // @available(iOS 14, *) at the call site.
            foreach (var targetGuid in new[] { frameworkTargetGuid, mainTargetGuid })
            {
                pbxProject.AddFrameworkToProject(targetGuid, "AppTrackingTransparency.framework", true);
                pbxProject.AddFrameworkToProject(targetGuid, "AdSupport.framework", false);
            }
            pbxProject.WriteToFile(projectPath);
#endif
        }
    }
}
