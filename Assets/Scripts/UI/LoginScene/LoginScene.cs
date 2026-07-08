using System;
using System.Text;
using Google;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.UI;
using Nakama;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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
    
    [SerializeField]
    private CanvasGroup loadingSceneCanvasGroup;

    // Object controller
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject buttonLayout;

    [SerializeField] private GameObject goLoadingHorizontal;
    [SerializeField] private GameObject goLoadingVertical;

    [Header("Google Sign-In")]
    [SerializeField] private string googleWebClientId = "546099158752-8bgak6biutovg9ke6qavt2aktstihbdk.apps.googleusercontent.com";

    private AppleAuthManager _appleAuthManager;

    private void Awake()
    {
        loadingSceneCanvasGroup.alpha = 1f;
        loadingSceneCanvasGroup.blocksRaycasts = true;
        loadingSceneCanvasGroup.interactable = true;
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        Debug.Log($"[LoginScene] btnGoogleLogin={btnGoogleLogin}, btnAppleLogin={btnAppleLogin}");
    }

    private void OnEnable()
    {
        loadingSceneCanvasGroup.alpha = 1f;
        loadingSceneCanvasGroup.blocksRaycasts = true;
        loadingSceneCanvasGroup.interactable = true;
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        buttonLayout.SetActive(true);
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
        buttonLayout.SetActive(true);
        btnLoginBD.gameObject.SetActive(true);
        btnAppleLogin.gameObject.SetActive(true);
        btnGoogleLogin.gameObject.SetActive(true);
        btnLoginGuest.gameObject.SetActive(true);
        // Gỡ overlay loading để lộ nút login — trước đây set lại alpha=1/blocksRaycasts=true
        // ở đây khiến overlay che vĩnh viễn buttonLayout vì OnInitSceneDataComplete (nơi gỡ
        // overlay) chỉ được bắn từ PvEBattleController trong MainBattleScene, không bao giờ
        // chạy lại khi đã quay về LoginScene.
        loadingSceneCanvasGroup.alpha = 1f;
        loadingSceneCanvasGroup.blocksRaycasts = true;
        loadingSceneCanvasGroup.interactable = true;
        
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

        // Apple Sign-In không khả dụng trên Android, ẩn luôn nút để tránh gây nhầm lẫn cho người dùng
        if (Application.platform == RuntimePlatform.Android && btnAppleLogin != null)
            btnAppleLogin.gameObject.SetActive(false);

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
        btnLoginBD.gameObject.SetActive(false);
        btnLoginGuest.gameObject.SetActive(false);
        btnAppleLogin.gameObject.SetActive(false);
        btnGoogleLogin.gameObject.SetActive(false);
    }

    private void OnClickLoginBackdrop()
    {
        loginPanel.SetActive(false);
        btnLoginBD.gameObject.SetActive(true);
        btnLoginGuest.gameObject.SetActive(true);
        btnGoogleLogin.gameObject.SetActive(true);
        if (Application.platform != RuntimePlatform.Android)
            btnAppleLogin.gameObject.SetActive(true);
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
            btnLogin.interactable = false;
            await NakamaClient.Instance.LoginAsync(username, password);
            loginPanel.gameObject.SetActive(false);
            await SceneManager.LoadSceneAsync("MainBattleScene");
            await GameBootstrap.Instance.RunAsync();
        }
        catch (ApiResponseException e)
        {
            Debug.LogError($"[LoginScene] Login failed ({e.StatusCode}): {e.Message}");
        }
        finally
        {
            btnLogin.interactable = true;
        }
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
            await GameBootstrap.Instance.RunAsync();
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
            await NakamaClient.Instance.AuthenticateDeviceAsync();
            Debug.Log($"[LoginScene] Guest login success. UserId={NakamaClient.Instance.Session.UserId}");
            await SceneManager.LoadSceneAsync("MainBattleScene");
            await GameBootstrap.Instance.RunAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoginScene] Guest login failed: {e.Message}");
        }
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
            var user = await GoogleSignIn.DefaultInstance.SignIn();
            Debug.Log($"[LoginScene] Google sign-in success. Email={user.Email}");

            await NakamaClient.Instance.AuthenticateGoogleAsync(user.IdToken);
            Debug.Log($"[LoginScene] Google Nakama auth success. UserId={NakamaClient.Instance.Session.UserId}");
            await SceneManager.LoadSceneAsync("MainBattleScene");
            await GameBootstrap.Instance.RunAsync();
        }
        catch (GoogleSignIn.SignInException e)
        {
            Debug.LogError($"[LoginScene] Google login failed. Status={e.Status} Message={e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoginScene] Google login failed: {e.GetType().Name} {e.Message}");
        }
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
            var tcs = new UniTaskCompletionSource<string>();

            _appleAuthManager.LoginWithAppleId(
                LoginOptions.IncludeEmail | LoginOptions.IncludeFullName,
                credential =>
                {
                    if (credential is IAppleIDCredential appleCredential)
                        tcs.TrySetResult(Encoding.UTF8.GetString(appleCredential.IdentityToken));
                    else
                        tcs.TrySetException(new Exception("Invalid Apple credential type"));
                },
                error => tcs.TrySetException(new Exception($"Apple Sign-In error: {error.LocalizedDescription}"))
            );

            var identityToken = await tcs.Task;
            await NakamaClient.Instance.AuthenticateAppleAsync(identityToken);
            Debug.Log($"[LoginScene] Apple Nakama auth success. UserId={NakamaClient.Instance.Session.UserId}");
            await SceneManager.LoadSceneAsync("MainBattleScene");
            await GameBootstrap.Instance.RunAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoginScene] Apple login failed: {e.Message}");
        }
    }
}
