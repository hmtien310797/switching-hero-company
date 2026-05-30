using System;
using System.Collections;
using Common;
using Google.Protobuf;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniTask = Cysharp.Threading.Tasks.UniTask;

public class LoginScene : MonoBehaviour
{
    // login
    [SerializeField] private TMP_InputField ipUsername;
    [SerializeField] private TMP_InputField ipPassword;
    [SerializeField] private Button btnLogin;
    [SerializeField] private Button btnLoginRegister;
    [SerializeField] private Button btnAppleLogin;
    [SerializeField] private Button btnGoogleLogin;
    // Register
    [SerializeField] private TMP_InputField ipUsernameRegister;
    [SerializeField] private TMP_InputField ipPasswordRegister;
    [SerializeField] private TMP_InputField ipPasswordConfirmRegister;
    [SerializeField] private Button btnRegister;
    [SerializeField] private Button btnBackToLogin;
    
    // Object controller
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;

    [SerializeField] private GameObject goLoadingHorizontal;
    [SerializeField] private GameObject goLoadingVertical;
    
    private const string LoginEndpoint = "v1/auth/login";
    private const string RegisterEngpoint = "v1/auth/register";

    private void Awake()
    {
        ScreenOrientationTracker.Instance.OnOrientationChanged += OnScreenOrientationChanged;
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        btnLogin?.onClick.AddListener(OnClickLogin);
        btnLoginRegister?.onClick.AddListener(OnClickRegister);
        btnBackToLogin?.onClick.AddListener(OnClickBackToLogin);
        btnRegister?.onClick.AddListener(OnClickSubmitRegister);
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

    private void OnDestroy()
    {
        ScreenOrientationTracker.Instance.OnOrientationChanged -= OnScreenOrientationChanged;
    }

    void Start()
    {
        OnScreenOrientationChanged(ScreenOrientationTracker.Instance.CurrentMode);
    }

    private void OnClickLogin()
    {
        DoLogin(ipUsername.text, ipPassword.text);
    }

    private void OnClickRegister()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
    }

    private void OnClickSubmitRegister()
    {
        StartCoroutine(DoRegister(ipUsernameRegister.text, ipPasswordRegister.text, ipPasswordConfirmRegister.text));
    }

    private void OnClickBackToLogin()
    {
        registerPanel.SetActive(false);
        loginPanel.SetActive(true);
    }

    private async UniTask DoLogin(string username, string password)
    {
        loginPanel.gameObject.SetActive(false);
        await SceneManager.LoadSceneAsync("MainBattleScene");
        await GameBootstrap.Instance.RunAsync();
        return;
        
        var request = new Login.LoginRequest
        {
            Meta = new Common.RequestMeta
            {
                RequestId = Guid.NewGuid().ToString(),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            },
            Username = username,
            Password = password
        };

        byte[] bodyRaw = request.ToByteArray();

        string url = $"{NetworkManager.Instance.BaseUrl}/{LoginEndpoint}";
        using var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/x-protobuf");

        await webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[LoginScene] Request failed: {webRequest.error}");
            return;
        }

        var response = Login.LoginResponse.Parser.ParseFrom(webRequest.downloadHandler.data);
        
        Debug.Log($"[LoginScene] Login success. UserId={response.UserId}");
        PlayerPrefs.SetString("auth_token", response.Token);
        PlayerPrefs.SetString("player_id", response.UserId);
        PlayerPrefs.Save();
        if (response.Meta.Code != 200)
        {
            Debug.LogError($"[LoginScene] Login error {response.Meta.Code}: {response.Meta.Message}");
            return;
        }
        // TODO: chuyển scene
        loginPanel.gameObject.SetActive(false);
        // await SceneManager.LoadSceneAsync("MainBattleScene");
        // await GameBootstrap.Instance.RunAsync();
        await SceneManager.LoadSceneAsync("MainBattleScene");
        await GameBootstrap.Instance.RunAsync();
    }

    private IEnumerator DoRegister(string username, string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            Debug.LogError("[LoginScene] Password and confirm password do not match");
            yield break;
        }

        var request = new Login.RegisterRequest
        {
            Meta = new Common.RequestMeta
            {
                RequestId = Guid.NewGuid().ToString(),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            },
            Username = username,
            Password = password
        };

        byte[] bodyRaw = request.ToByteArray();

        string url = $"{NetworkManager.Instance.BaseUrl}/{RegisterEngpoint}";
        using var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/x-protobuf");

        yield return webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[LoginScene] Register request failed: {webRequest.error}");
            yield break;
        }

        var response = Login.RegisterRespone.Parser.ParseFrom(webRequest.downloadHandler.data);

        if (response.Meta.Code != 0)
        {
            Debug.LogError($"[LoginScene] Register error {response.Meta.Code}: {response.Meta.Message}");
            yield break;
        }

        Debug.Log($"[LoginScene] Register success. Status={response.Status}");
        if (response.Status == 1) // TODO: chuyển về login panel
        {
            registerPanel.SetActive(false);
            loginPanel.SetActive(true);
        } // success 
        
    }
}
