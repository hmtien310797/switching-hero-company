using System;
using System.Threading.Tasks;
using Nakama;
using Newtonsoft.Json;
using UnityEngine;

public class NakamaClient : MonoBehaviour
{
    private const string SessionPrefKey = "nakama.session";
    private const string SingletonName = "[NakamaClient]";

    [Header("Server Config")]
    [SerializeField] private string scheme = "http";
    [SerializeField] private string host = "171.244.44.71";
    [SerializeField] private int port = 7350;
    [SerializeField] private string serverKey = "defaultkey";

    private static NakamaClient _instance;

    public static NakamaClient Instance
    {
        get
        {
            if (_instance != null) return _instance;
            var go = GameObject.Find(SingletonName) ?? new GameObject(SingletonName);
            _instance = go.GetComponent<NakamaClient>() ?? go.AddComponent<NakamaClient>();
            DontDestroyOnLoad(go);
            return _instance;
        }
    }

    public IClient Client { get; private set; }
    public ISocket Socket { get; private set; }
    public ISession Session { get; private set; }

    public bool IsSocketConnected => Socket?.IsConnected ?? false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        Client = new Client(scheme, host, port, serverKey)
        {
#if UNITY_EDITOR
            Logger = new UnityLogger()
#endif
        };
        Socket = Client.NewSocket();

        Socket.Connected += () => Debug.Log("[NakamaClient] Socket connected.");
        Socket.Closed += (reason) => Debug.Log($"[NakamaClient] Socket closed: {reason}");
        Socket.ReceivedError += ex => Debug.LogError($"[NakamaClient] Socket error: {ex.Message}");

        TryRestoreSession();
    }

    private void TryRestoreSession()
    {
        var token = PlayerPrefs.GetString(SessionPrefKey, null);
        if (string.IsNullOrEmpty(token)) return;

        var session = Nakama.Session.Restore(token);
        if (session != null && !session.HasExpired(DateTime.UtcNow))
        {
            Session = session;
            Debug.Log($"[NakamaClient] Session restored. UserId={session.UserId}");
        }
    }

    /// <summary>
    /// Authenticate guest bằng device ID — tạo account mới nếu chưa có.
    /// </summary>
    public async Task<ISession> AuthenticateDeviceAsync() // Guest 
    {
        var deviceId = PlayerPrefs.GetString("nakama.deviceid", null);
        if (string.IsNullOrEmpty(deviceId))
        {
            deviceId = SystemInfo.deviceUniqueIdentifier;
            if (deviceId == SystemInfo.unsupportedIdentifier)
                deviceId = Guid.NewGuid().ToString();
            PlayerPrefs.SetString("nakama.deviceid", deviceId);
            PlayerPrefs.Save();
        }

        Session = await Client.AuthenticateDeviceAsync(deviceId);
        PlayerPrefs.SetString(SessionPrefKey, Session.AuthToken);
        PlayerPrefs.Save();
        Debug.Log($"[NakamaClient] Guest authenticated. UserId={Session.UserId}");
        return Session;
    }

    /// <summary>
    /// Authenticate với Nakama bằng custom ID (thường là userId từ hệ thống auth của game).
    /// </summary>
    public async Task<ISession> AuthenticateCustomAsync(string customId, bool create = true)
    {
        Session = await Client.AuthenticateCustomAsync(customId, null, create);
        PlayerPrefs.SetString(SessionPrefKey, Session.AuthToken);
        PlayerPrefs.Save();
        Debug.Log($"[NakamaClient] Authenticated. UserId={Session.UserId}");
        return Session;
    }

    /// <summary>
    /// Authenticate bằng Email + Password. create=true để tự tạo account nếu chưa có.
    /// </summary>
    public async Task<ISession> AuthenticateEmailAsync(string email, string password, bool create = false)
    {
        Session = await Client.AuthenticateEmailAsync(email, password, null, create);
        PlayerPrefs.SetString(SessionPrefKey, Session.AuthToken);
        PlayerPrefs.Save();
        Debug.Log($"[NakamaClient] Email authenticated. UserId={Session.UserId}");
        return Session;
    }

    /// <summary>
    /// Authenticate bằng Google. googleToken là idToken lấy từ Google Sign-In SDK.
    /// </summary>
    public async Task<ISession> AuthenticateGoogleAsync(string googleToken)
    {
        Session = await Client.AuthenticateGoogleAsync(googleToken);
        PlayerPrefs.SetString(SessionPrefKey, Session.AuthToken);
        PlayerPrefs.Save();
        Debug.Log($"[NakamaClient] Google authenticated. UserId={Session.UserId}");
        return Session;
    }

    /// <summary>
    /// Authenticate bằng Apple. appleToken là identityToken lấy từ Sign In with Apple SDK (iOS only).
    /// </summary>
    public async Task<ISession> AuthenticateAppleAsync(string appleToken)
    {
        Session = await Client.AuthenticateAppleAsync(appleToken);
        PlayerPrefs.SetString(SessionPrefKey, Session.AuthToken);
        PlayerPrefs.Save();
        Debug.Log($"[NakamaClient] Apple authenticated. UserId={Session.UserId}");
        return Session;
    }

    /// <summary>
    /// Set session từ bên ngoài (ví dụ sau khi auth qua hệ thống custom).
    /// </summary>
    public void SetSession(ISession session)
    {
        Session = session;
        PlayerPrefs.SetString(SessionPrefKey, session.AuthToken);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Kết nối WebSocket. Phải có Session trước khi gọi.
    /// </summary>
    public async Task ConnectSocketAsync()
    {
        if (Session == null)
        {
            Debug.LogError("[NakamaClient] Cannot connect socket: no active session.");
            return;
        }
        if (IsSocketConnected) return;

        await Socket.ConnectAsync(Session);
    }

    public async Task DisconnectSocketAsync()
    {
        if (IsSocketConnected)
            await Socket.CloseAsync();
    }

    public void ClearSession()
    {
        Session = null;
        PlayerPrefs.DeleteKey(SessionPrefKey);
        PlayerPrefs.Save();
    }

    // ── Hero ──────────────────────────────────────────────────────────────────

    public async Task<HeroListResponse> GetHeroListAsync()
    {
        var response = await Client.RpcAsync(Session, "hero/list");
        return JsonConvert.DeserializeObject<HeroListResponse>(response.Payload);
    }

    /// <summary>
    /// Update lineup. Pass hero uid or null for an empty slot.
    /// </summary>
    public async Task SetLineupAsync(string slot0, string slot1)
    {
        var payload = JsonConvert.SerializeObject(new SetLineupRequest
        {
            Lineup = new[] { slot0, slot1 }
        });
        await Client.RpcAsync(Session, "hero/set_lineup", payload);
    }

    // ── Bag ───────────────────────────────────────────────────────────────────

    public async Task<BagResponse> GetBagAsync()
    {
        var response = await Client.RpcAsync(Session, "bag/get");
        return JsonConvert.DeserializeObject<BagResponse>(response.Payload);
    }

    // ── Player ────────────────────────────────────────────────────────────────

    public async Task<PlayerMeResponse> GetPlayerMeAsync()
    {
        var response = await Client.RpcAsync(Session, "player/me");
        return JsonUtility.FromJson<PlayerMeResponse>(response.Payload);
    }

    public async Task<PlayerUpdateResponse> UpdatePlayerAsync(PlayerUpdateRequest request)
    {
        var payload = JsonUtility.ToJson(request);
        var response = await Client.RpcAsync(Session, "player/update", payload);
        return JsonUtility.FromJson<PlayerUpdateResponse>(response.Payload);
    }

    private async void OnApplicationQuit()
    {
        await DisconnectSocketAsync();
    }
}
