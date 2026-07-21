using System;
using System.Text;
using System.Threading;
using Google;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.RemoteUpdate;
using Immortal_Switch.Scripts.UI;
using Nakama;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Shared.Views;
using UniTask = Cysharp.Threading.Tasks.UniTask;

[DefaultExecutionOrder(-100)]
public class LoginScene : MonoBehaviour
{
    // login
    [SerializeField] private TMP_InputField ipUsername;
    [SerializeField] private TMP_InputField ipPassword;
    [SerializeField] private Button btnLogin; // login user pass
    [SerializeField] private Button btnLoginRegister;
    [SerializeField] private Button btnAppleLogin;
    [SerializeField] private Button btnGoogleLogin;
    [SerializeField] private Button btnLoginBD;
    [SerializeField] private Button btnLoginGuest;
    [SerializeField] private Button btnLoginBackdrop; // touch ra ngoài loginPanel để đóng
    // Register
    [SerializeField] private TMP_InputField ipUsernameRegister;
    [SerializeField] private TMP_InputField ipPasswordRegister;
    [SerializeField] private TMP_InputField ipPasswordConfirmRegister;
    [SerializeField] private Button btnRegister;
    [SerializeField] private Button btnBackToLogin;
    
    [SerializeField] private TMP_Text progressTextVertical;
    [SerializeField] private TMP_Text progressTextHorizontal;
    
    [SerializeField] private UI.LoginScene.SliderStarFollower sliderStarFollowerVertical;
    [SerializeField] private UI.LoginScene.SliderStarFollower sliderStarFollowerHorizontal;
    
    [SerializeField]
    private CanvasGroup loadingSceneCanvasGroup;

    [Header("Remote Localization")]
    [SerializeField, Tooltip("Label gắn cho các Localization table cần dùng ở Login Scene.")]
    private string localizationPreloadLabel = "Preload";

    private bool _isInitializingLoginLocalization;

    // Object controller
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject buttonLayout;

    [SerializeField] private GameObject goLoadingHorizontal;
    [SerializeField] private GameObject goLoadingVertical;

    [Header("Google Sign-In")]
    [SerializeField] private string googleWebClientId = "546099158752-8bgak6biutovg9ke6qavt2aktstihbdk.apps.googleusercontent.com";

    private AppleAuthManager _appleAuthManager;
    
    private async UniTask RunBootstrapAsync()
    {
        var cancellationToken = this.GetCancellationTokenOnDestroy();

        // Phase 1: Addressables chạy riêng từ 0 -> 100%.
        ResetLoadingProgress();

        await GameBootstrap.Instance.RunRemoteUpdateAsync(
            OnBootstrapProgress,
            cancellationToken
        );

        // Giữ 100% một chút để người chơi thấy phase Addressables đã hoàn tất.
        OnBootstrapProgress(1f, "Content ready");

        await UniTask.Delay(
            TimeSpan.FromMilliseconds(1000),
            DelayType.UnscaledDeltaTime,
            PlayerLoopTiming.Update,
            cancellationToken
        );

        // Phase 2: reset về 0 rồi chạy riêng tiến trình init game.
        ResetLoadingProgress();
        OnBootstrapProgress(0f, "Preparing game data");

        await GameBootstrap.Instance.RunAsync(
            OnBootstrapProgress,
            cancellationToken
        );
    }

    private void ResetLoadingProgress()
    {
        sliderStarFollowerVertical?.ResetProgress();
        sliderStarFollowerHorizontal?.ResetProgress();

        if (progressTextVertical != null)
        {
            progressTextVertical.text = string.Empty;
        }

        if (progressTextHorizontal != null)
        {
            progressTextHorizontal.text = string.Empty;
        }
    }
    
    private void OnBootstrapProgress(
        float progress,
        string message)
    {
        sliderStarFollowerVertical?.PlayTo(progress);
        sliderStarFollowerHorizontal?.PlayTo(progress);

        if (progressTextVertical != null)
        {
            progressTextVertical.text = message;
        }

        if (progressTextHorizontal != null)
        {
            progressTextHorizontal.text = message;
        }
    }

    private void OnInitSceneDataComplete()
    {
        buttonLayout.SetActive(false);
        loadingSceneCanvasGroup.DOFade(0f, 0.5f).SetEase(Ease.Linear);
        loadingSceneCanvasGroup.blocksRaycasts = false;
        loadingSceneCanvasGroup.interactable = false;
    }
    
    private void OnUserLogOut()
    {
        InitializeLoginLocalizationAsync().Forget();
    }

    private void OnScreenOrientationChanged(ScreenOrientationTracker.ScreenViewMode obj)
    {
        if (obj == ScreenOrientationTracker.ScreenViewMode.Landscape)
        {
            goLoadingVertical.SetActive(false);
            goLoadingHorizontal.SetActive(true);
        }
        else
        {
            goLoadingVertical.SetActive(true);
            goLoadingHorizontal.SetActive(false);
        }
    }

    void Start()
    {
        OnScreenOrientationChanged(ScreenOrientationTracker.Instance.CurrentMode);
        if (AppleAuthManager.IsCurrentPlatformSupported)
            _appleAuthManager = new AppleAuthManager(new PayloadDeserializer());

        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            WebClientId = googleWebClientId,
            RequestIdToken = true,
            UseGameSignIn = false
        };

        // Quay về LoginScene sau khi bị server force-logout (vd: login ở thiết bị khác) —
        // NakamaClient lưu lý do trước khi load lại scene này, đọc 1 lần rồi xoá.
        var reason = NakamaClient.Instance.LastForceLogoutReason;
        if (!string.IsNullOrEmpty(reason))
        {
            NakamaClient.Instance.LastForceLogoutReason = null;
            Debug.LogWarning($"[LoginScene] Đã đăng xuất: {reason}");
        }
        
        GameEventManager.Subscribe(GameEvents.OnInitSceneDataComplete, OnInitSceneDataComplete);
        GameEventManager.Subscribe(GameEvents.OnUserLogOut, OnUserLogOut);
        ScreenOrientationTracker.Instance.OnOrientationChanged += OnScreenOrientationChanged;
        
        btnGoogleLogin?.onClick.AddListener(OnClickGoogleLogin);
        btnAppleLogin?.onClick.AddListener(OnClickAppleLogin);
        btnLogin?.onClick.AddListener(OnClickLogin);
        btnLoginRegister?.onClick.AddListener(OnClickRegister);
        btnBackToLogin?.onClick.AddListener(OnClickBackToLogin);
        btnRegister?.onClick.AddListener(OnClickSubmitRegister);
        btnLoginBD?.onClick.AddListener(OnClickLoginBD);
        btnLoginGuest?.onClick.AddListener(OnClickLoginGuest);
        btnLoginBackdrop?.onClick.AddListener(OnClickLoginBackdrop);
        InitializeLoginLocalizationAsync().Forget();
    }

    private async UniTask InitializeLoginLocalizationAsync()
    {
        if (_isInitializingLoginLocalization)
        {
            return;
        }

        btnAppleLogin.gameObject.SetActive(!(Application.platform == RuntimePlatform.Android && btnAppleLogin != null));
        _isInitializingLoginLocalization = true;
        buttonLayout.SetActive(false);
        loadingSceneCanvasGroup.alpha = 1f;
        loadingSceneCanvasGroup.blocksRaycasts = true;
        loadingSceneCanvasGroup.interactable = true;
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        Debug.Log($"[LoginScene] btnGoogleLogin={btnGoogleLogin}, btnAppleLogin={btnAppleLogin}");

        var cancellationToken = this.GetCancellationTokenOnDestroy();

        try
        {
            // Nếu Localization đã được load trong phiên chơi trước, phải release table/bundle
            // cũ TRƯỚC Addressables.UpdateCatalogs để tránh lỗi same files already loaded.
            await LocalizationManager.Instance
                .PrepareForRemoteCatalogUpdateAsync(cancellationToken);

            var progressHandler = new LoginLocalizationProgressHandler(
                OnLoginLocalizationProgress);

            await AddressableRemoteUpdateService.Instance
                .DebugPrintRemoteUrlAsync(
                    "Preload",
                    cancellationToken);
            
            var result = await AddressableRemoteUpdateService.Instance
                .CheckAndDownloadLabelAsync(
                    localizationPreloadLabel,
                    progressHandler,
                    cancellationToken);

            if (result.Status == RemoteContentUpdateStatus.Cancelled)
            {
                PopupConfirmService.ShowNotice("Error", "Can not check remote asset, please check internet connection", () => InitializeLoginLocalizationAsync().Forget(),
                    "OK");
                
                return;
            }

            if (!result.IsSuccess)
            {
                // Không chặn Login khi mất mạng/timeout. LocalizationManager vẫn thử
                // dùng table local hoặc bundle đã cache từ lần chạy trước.
                Debug.LogWarning(
                    $"[LoginScene] Remote localization unavailable ({result.Status}). " +
                    "Falling back to local/cached localization.");
            }

            await LocalizationManager.Instance
                .ReloadRemoteLocalizationAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            PopupConfirmService.ShowNotice("Error", "Can not check remote asset, please check internet connection", () => InitializeLoginLocalizationAsync().Forget(),
                "OK");
            return;
        }
        catch (Exception ex)
        {
            // Fail-open: login vẫn phải sử dụng được nếu CDN/localization lỗi.
            Debug.LogError($"[LoginScene] Localization startup failed: {ex}");

            try
            {
                await LocalizationManager.Instance.InitializeAsync();
            }
            catch (Exception fallbackEx)
            {
                PopupConfirmService.ShowNotice("Error", "Can not check remote asset, please check internet connection", () => InitializeLoginLocalizationAsync().Forget(),
                    "OK");
                Debug.LogError(
                    $"[LoginScene] Local localization fallback failed: {fallbackEx}");
                return;
            }
        }
        _isInitializingLoginLocalization = false;
        buttonLayout.SetActive(true);
    }

    private void OnLoginLocalizationProgress(
        RemoteContentUpdateProgress progress)
    {
        sliderStarFollowerVertical?.PlayTo(progress.Percent);
        sliderStarFollowerHorizontal?.PlayTo(progress.Percent);

        if (progressTextVertical != null)
        {
            progressTextVertical.text = progress.CurrentLabel;
        }

        if (progressTextHorizontal != null)
        {
            progressTextHorizontal.text = progress.CurrentLabel;
        }
    }

    private sealed class LoginLocalizationProgressHandler
        : IRemoteUpdateProgressHandler
    {
        private readonly Action<RemoteContentUpdateProgress> _onProgress;

        public LoginLocalizationProgressHandler(
            Action<RemoteContentUpdateProgress> onProgress)
        {
            _onProgress = onProgress;
        }

        public void OnProgress(RemoteContentUpdateProgress progress)
        {
            _onProgress?.Invoke(progress);
        }

        public void OnComplete(RemoteContentUpdateResult result)
        {
            // Kết quả cuối được xử lý sau await trong InitializeLoginLocalizationAsync.
        }
    }

    private void Update()
    {
        _appleAuthManager?.Update();
    }

    private void OnClickLogin()
    {
        var badwordMatches = IllegalWordDetection.DetectIllegalWords(ipUsername.text);

        if (badwordMatches.Count > 0)
        {
            foreach (var match in badwordMatches)
            {
                var matchedWord = ipUsername.text.Substring(match.Key, match.Value);
                Debug.LogError($"[LoginScene] Username \"{ipUsername.text}\" bị chặn do khớp badword \"{matchedWord}\" tại vị trí {match.Key} (dài {match.Value})");
            }
            return;
        }

        DoLogin(ipUsername.text, ipPassword.text).Forget();
    }

    private void OnClickRegister()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
    }

    private void OnClickLoginBD()
    {
        Debug.Log("[LoginScene] Login with BD");
        loginPanel.SetActive(true);
        buttonLayout.SetActive(false);
    }

    private void OnClickLoginBackdrop()
    {
        loginPanel.SetActive(false);
        buttonLayout.SetActive(false);
    }

    private void OnClickSubmitRegister()
    {
        var badwordMatches = IllegalWordDetection.DetectIllegalWords(ipUsernameRegister.text);

        if (badwordMatches.Count > 0)
        {
            foreach (var match in badwordMatches)
            {
                var matchedWord = ipUsernameRegister.text.Substring(match.Key, match.Value);
                Debug.LogError($"[LoginScene] Username đăng ký \"{ipUsernameRegister.text}\" bị chặn do khớp badword \"{matchedWord}\" tại vị trí {match.Key} (dài {match.Value})");
            }
            return;
        }

        DoRegister(ipUsernameRegister.text, ipPasswordRegister.text, ipPasswordConfirmRegister.text).Forget();
    }

    private void OnClickBackToLogin()
    {
        registerPanel.SetActive(false);
        loginPanel.SetActive(true);
    }

    private async UniTask DoLogin(string username, string password)
    {
        try
        {
            loginPanel.SetActive(false);
            buttonLayout.SetActive(false);
            await NakamaClient.Instance.LoginAsync(username, password);
        }
        catch (ApiResponseException e)
        {
            PopupConfirmService.ShowNotice("Error", "Login failed, please try again", () => buttonLayout.SetActive(true),
                "OK");
            Debug.LogError($"[LoginScene] Login failed ({e.StatusCode}): {e.Message}");
            return;
        }
        
        await SceneManager.LoadSceneAsync("MainBattleScene");
        await RunBootstrapAsync();
    }

    private async UniTask DoRegister(string username, string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            Debug.LogError("[LoginScene] Password and confirm password do not match");
            return;
        }

        try
        {
            btnRegister.interactable = false;
            await NakamaClient.Instance.RegisterAsync(username, password);
            await NakamaClient.Instance.LoginAsync(username, password);
            registerPanel.SetActive(false);
            await SceneManager.LoadSceneAsync("MainBattleScene");
            await RunBootstrapAsync();
        }
        catch (ApiResponseException e)
        {
            Debug.LogError($"[LoginScene] Register failed ({e.StatusCode}): {e.Message}");
        }
        finally
        {
            btnRegister.interactable = true;
        }
    }

    private void OnClickLoginGuest()
    {
        DoLoginGuest().Forget();
    }

    private async UniTask DoLoginGuest()
    {
        try
        {
            buttonLayout.SetActive(false);
            await NakamaClient.Instance.AuthenticateDeviceAsync();
        }
        catch (Exception e)
        {
            PopupConfirmService.ShowNotice("Error", "Can not login guest, please try again", () => buttonLayout.SetActive(true),
                "OK");
            Debug.LogError($"[LoginScene] Guest login failed: {e.Message}");
            return;
        }
        
        Debug.Log($"[LoginScene] Guest login success. UserId={NakamaClient.Instance.Session.UserId}");
        await SceneManager.LoadSceneAsync("MainBattleScene");
        await RunBootstrapAsync();
    }

    public void OnClickGoogleLogin()
    {
        Debug.Log("[LoginScene] Login with Google");
        DoLoginGoogle().Forget();
    }

    private async UniTask DoLoginGoogle()
    {
        try
        {
            buttonLayout.SetActive(false);
            var user = await GoogleSignIn.DefaultInstance.SignIn();
            if (user == null)
            {
                buttonLayout.SetActive(true);
                return;
            }
            Debug.Log($"[LoginScene] Google sign-in success. Email={user.Email}");

            await NakamaClient.Instance.AuthenticateGoogleAsync(user.IdToken);
        }
        catch (GoogleSignIn.SignInException e)
        {
            PopupConfirmService.ShowNotice("Error", "Can not login google, please try again", () => buttonLayout.SetActive(true),
                "OK");
            Debug.LogError($"[LoginScene] Google login failed. Status={e.Status} Message={e.Message}");
            return;
        }
        catch (Exception e)
        {
            PopupConfirmService.ShowNotice("Error", "Can not login google, please try again", () => buttonLayout.SetActive(true),
                "OK");
            Debug.LogError($"[LoginScene] Google login failed: {e.GetType().Name} {e.Message}");
            return;
        }
        
        Debug.Log($"[LoginScene] Google Nakama auth success. UserId={NakamaClient.Instance.Session.UserId}");
        await SceneManager.LoadSceneAsync("MainBattleScene");
        await RunBootstrapAsync();
    }

    public void OnClickAppleLogin()
    {
        Debug.Log("[LoginScene] Login with Apple");
        DoLoginApple().Forget();
    }

    private async UniTask DoLoginApple()
    {
        if (_appleAuthManager == null)
        {
            Debug.LogError("[LoginScene] Apple Sign-In is not supported on this platform");
            return;
        }

        try
        {
            buttonLayout.SetActive(false);
            var tcs = new UniTaskCompletionSource<string>();

            _appleAuthManager.LoginWithAppleId(
                LoginOptions.IncludeEmail | LoginOptions.IncludeFullName,
                credential =>
                {
                    if (credential is IAppleIDCredential appleCredential)
                        tcs.TrySetResult(Encoding.UTF8.GetString(appleCredential.IdentityToken));
                    else
                    {
                        tcs.TrySetException(new Exception("Invalid Apple credential type"));
                    }
                },
                error =>
                {
                    tcs.TrySetException(new Exception($"Apple Sign-In error: {error.LocalizedDescription}"));
                });

            var identityToken = await tcs.Task;
            await NakamaClient.Instance.AuthenticateAppleAsync(identityToken);
        }
        catch (Exception e)
        {
            PopupConfirmService.ShowNotice("Error", "Can not login apple, please try again", () => buttonLayout.SetActive(true),
                "OK");
            Debug.LogError($"[LoginScene] Apple login failed: {e.Message}");
            return;
        }
        
        Debug.Log($"[LoginScene] Apple Nakama auth success. UserId={NakamaClient.Instance.Session.UserId}");
        await SceneManager.LoadSceneAsync("MainBattleScene");
        await RunBootstrapAsync();
    }
    private void OnDestroy()
    {
        GameEventManager.Unsubscribe(GameEvents.OnInitSceneDataComplete, OnInitSceneDataComplete);
        GameEventManager.Unsubscribe(GameEvents.OnUserLogOut, OnUserLogOut);

        if (ScreenOrientationTracker.Instance != null)
        {
            ScreenOrientationTracker.Instance.OnOrientationChanged -= OnScreenOrientationChanged;
        }

        btnGoogleLogin?.onClick.RemoveListener(OnClickGoogleLogin);
        btnAppleLogin?.onClick.RemoveListener(OnClickAppleLogin);
        btnLogin?.onClick.RemoveListener(OnClickLogin);
        btnLoginRegister?.onClick.RemoveListener(OnClickRegister);
        btnBackToLogin?.onClick.RemoveListener(OnClickBackToLogin);
        btnRegister?.onClick.RemoveListener(OnClickSubmitRegister);
        btnLoginBD?.onClick.RemoveListener(OnClickLoginBD);
        btnLoginGuest?.onClick.RemoveListener(OnClickLoginGuest);
        btnLoginBackdrop?.onClick.RemoveListener(OnClickLoginBackdrop);

        loadingSceneCanvasGroup?.DOKill();
    }

}
