using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Immortal_Switch.Scripts.Core
{
    /// <summary>
    /// Requests the iOS App Tracking Transparency (ATT) permission. Apple requires this
    /// prompt before any IDFA-reading SDK (ad networks, attribution) is initialized, so
    /// call <see cref="RequestAuthorization"/> once at app startup, before those SDKs.
    /// No-op (reports <see cref="ATTStatus.NotAvailable"/>) outside iOS and on iOS &lt; 14,
    /// since the ATT dialog doesn't exist there.
    /// </summary>
    public class AppTrackingTransparencyBridge : MonoBehaviour
    {
        public enum ATTStatus
        {
            NotAvailable = -1,
            NotDetermined = 0,
            Restricted = 1,
            Denied = 2,
            Authorized = 3,
        }

        // Must match the GameObject/method names the native side targets via
        // UnitySendMessage in Assets/Plugins/iOS/AppTrackingTransparency/AppTrackingTransparency.mm.
        private const string GameObjectName = "AppTrackingTransparencyBridge";

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int _ATT_GetAuthorizationStatus();

        [DllImport("__Internal")]
        private static extern void _ATT_RequestAuthorization();
#endif

        private static AppTrackingTransparencyBridge _instance;
        private static Action<ATTStatus> _pendingCallback;

        public static ATTStatus CurrentStatus
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return (ATTStatus)_ATT_GetAuthorizationStatus();
#else
                return ATTStatus.NotAvailable;
#endif
            }
        }

        /// <summary>
        /// Shows the system ATT prompt if the user hasn't been asked yet; calls back
        /// immediately with the existing status otherwise. Safe to call unconditionally
        /// on every platform/version — non-iOS-14+ targets get an immediate callback.
        /// </summary>
        public static void RequestAuthorization(Action<ATTStatus> onComplete)
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (_instance == null)
            {
                var go = new GameObject(GameObjectName);
                _instance = go.AddComponent<AppTrackingTransparencyBridge>();
                DontDestroyOnLoad(go);
            }
            _pendingCallback = onComplete;
            _ATT_RequestAuthorization();
#else
            onComplete?.Invoke(ATTStatus.NotAvailable);
#endif
        }

        // Invoked by native code via UnitySendMessage(GameObjectName, nameof(OnAuthorizationStatusReceived), status).
        public void OnAuthorizationStatusReceived(string statusString)
        {
            var status = int.TryParse(statusString, out var raw) ? (ATTStatus)raw : ATTStatus.NotAvailable;
            var callback = _pendingCallback;
            _pendingCallback = null;
            callback?.Invoke(status);
        }
    }
}
