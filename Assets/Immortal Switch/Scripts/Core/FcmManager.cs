using System;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Messaging;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Immortal_Switch.Scripts.Core
{
    /// <summary>
    /// Đăng ký FCM device token lên server (RPC player/register_fcm_token) — phục vụ push nhắc
    /// AFK ≥ 12h (xem nakama/src/handler/fcm.js). Chỉ lo đăng ký/refresh token; KHÔNG xử lý hiển
    /// thị khi nhận message lúc app đang mở — push loại này chỉ có ý nghĩa khi app đang đóng/nền,
    /// nằm ngoài phạm vi tính năng hiện tại.
    /// </summary>
    public class FcmManager : Singleton<FcmManager>
    {
        private bool _initialized;

        // Instance thường được tạo động (không qua prefab đã set DontDestroyOnLoadEnabled trong
        // Inspector) — set thẳng ở đây để không mất listener TokenReceived khi đổi scene.
        protected override void OnSingletonAwake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public override async UniTask InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;

            RequestNotificationPermissionIfNeeded();

            DependencyStatus dependencyStatus;
            try
            {
                dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FcmManager] CheckAndFixDependenciesAsync failed: {ex.Message}");
                return;
            }

            if (dependencyStatus != DependencyStatus.Available)
            {
                Debug.LogWarning($"[FcmManager] Firebase dependencies not available: {dependencyStatus}");
                return;
            }

            // Fires whenever Firebase (re)generates a token — e.g. rotation, app data cleared —
            // not just once at startup, so the server always has the latest token.
            FirebaseMessaging.TokenReceived += OnTokenReceived;

            try
            {
                var token = await FirebaseMessaging.GetTokenAsync();
                await SendTokenToServerAsync(token);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FcmManager] GetTokenAsync failed: {ex.Message}");
            }
        }

        private void OnTokenReceived(object sender, TokenReceivedEventArgs e)
        {
            SendTokenToServerAsync(e.Token).Forget();
        }

        /// <summary>Firebase callbacks/Tasks (CheckAndFixDependenciesAsync, GetTokenAsync,
        /// TokenReceived) can all complete/fire off the main thread — SwitchToMainThread before
        /// touching NakamaClient.Instance, same class of bug as Socket.ReceivedNotification
        /// earlier this session (NakamaClient.cs HandleReceivedNotificationAsync).</summary>
        private async UniTask SendTokenToServerAsync(string token)
        {
            if (string.IsNullOrEmpty(token)) return;

            await UniTask.SwitchToMainThread();

            if (!NakamaClient.Instance.IsLoggedIn) return;

            try
            {
                await NakamaClient.Instance.RegisterFcmTokenAsync(token, CurrentPlatform);
                Debug.Log("[FcmManager] FCM token registered.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FcmManager] RegisterFcmTokenAsync failed: {ex.Message}");
            }
        }

        private static string CurrentPlatform
        {
            get
            {
#if UNITY_IOS
                return "ios";
#elif UNITY_ANDROID
                return "android";
#else
                return "unknown";
#endif
            }
        }

        private static void RequestNotificationPermissionIfNeeded()
        {
#if UNITY_ANDROID
            const string permission = "android.permission.POST_NOTIFICATIONS";
            if (!Permission.HasUserAuthorizedPermission(permission))
            {
                Permission.RequestUserPermission(permission);
            }
#endif
        }
    }
}
