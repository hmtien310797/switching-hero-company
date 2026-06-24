using System;
using System.Threading.Tasks;
using Nakama;
using Newtonsoft.Json;
using UnityEngine;

public class NakamaClient : MonoBehaviour
{
    private const string SessionPrefKey        = "nakama.session";
    private const string RefreshTokenPrefKey   = "nakama.refreshtoken";
    private const string SingletonName         = "[NakamaClient]";

    [Header("Server Config")]
    [SerializeField] private string scheme = "http";
    //[SerializeField] private string host = "171.244.44.71"; // dev env
    [SerializeField] private string host = "192.168.8.82"; // local env
    [SerializeField] private int port = 7350;
    [SerializeField] private string serverKey = "switchinghero-server-key";
    [SerializeField] private string httpKey = "switchinghero-http-key";

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
    public bool IsLoggedIn         => Session != null && !Session.IsExpired;

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

        _ = TryRestoreSessionAsync();
    }

    private void SaveSession(ISession session)
    {
        PlayerPrefs.SetString(SessionPrefKey, session.AuthToken);
        PlayerPrefs.SetString(RefreshTokenPrefKey, session.RefreshToken ?? "");
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Khôi phục session đã lưu khi app khởi động.
    /// Nếu AuthToken hết hạn nhưng RefreshToken còn hạn → tự động gia hạn.
    /// </summary>
    public async Task TryRestoreSessionAsync()
    {
        var authToken    = PlayerPrefs.GetString(SessionPrefKey, null);
        var refreshToken = PlayerPrefs.GetString(RefreshTokenPrefKey, null);
        if (string.IsNullOrEmpty(authToken)) return;

        var session = Nakama.Session.Restore(authToken, refreshToken);
        if (session == null) return;

        if (!session.HasExpired(DateTime.UtcNow))
        {
            Session = session;
            Debug.Log($"[NakamaClient] Session restored. UserId={session.UserId}");
            return;
        }

        // AuthToken hết hạn — thử gia hạn bằng RefreshToken
        try
        {
            Session = session; // cần gán trước để RefreshSessionAsync dùng được
            await RefreshSessionAsync();
            Debug.Log($"[NakamaClient] Session auto-refreshed. UserId={Session.UserId}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[NakamaClient] Session refresh failed, require re-login: {e.Message}");
            ClearSession();
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
        SaveSession(Session);
        Debug.Log($"[NakamaClient] Guest authenticated. UserId={Session.UserId}");
        return Session;
    }

    /// <summary>
    /// Authenticate với Nakama bằng custom ID (thường là userId từ hệ thống auth của game).
    /// </summary>
    public async Task<ISession> AuthenticateCustomAsync(string customId, bool create = true)
    {
        Session = await Client.AuthenticateCustomAsync(customId, null, create);
        SaveSession(Session);
        Debug.Log($"[NakamaClient] Authenticated. UserId={Session.UserId}");
        return Session;
    }

    /// <summary>
    /// Authenticate bằng Email + Password. create=true để tự tạo account nếu chưa có.
    /// </summary>
    public async Task<ISession> AuthenticateEmailAsync(string email, string password, bool create = false)
    {
        Session = await Client.AuthenticateEmailAsync(email, password, null, create);
        SaveSession(Session);
        Debug.Log($"[NakamaClient] Email authenticated. UserId={Session.UserId}");
        return Session;
    }

    /// <summary>
    /// Authenticate bằng Google. googleToken là idToken lấy từ Google Sign-In SDK.
    /// </summary>
    public async Task<ISession> AuthenticateGoogleAsync(string googleToken)
    {
        Session = await Client.AuthenticateGoogleAsync(googleToken);
        SaveSession(Session);
        Debug.Log($"[NakamaClient] Google authenticated. UserId={Session.UserId}");
        return Session;
    }

    /// <summary>
    /// Authenticate bằng Apple. appleToken là identityToken lấy từ Sign In with Apple SDK (iOS only).
    /// </summary>
    public async Task<ISession> AuthenticateAppleAsync(string appleToken)
    {
        Session = await Client.AuthenticateAppleAsync(appleToken);
        SaveSession(Session);
        Debug.Log($"[NakamaClient] Apple authenticated. UserId={Session.UserId}");
        return Session;
    }

    /// <summary>
    /// Set session từ bên ngoài (ví dụ sau khi auth qua hệ thống custom).
    /// </summary>
    public void SetSession(ISession session)
    {
        Session = session;
        SaveSession(session);
    }

    // ── Auth/Register + Login ──────────────────────────────────────────────────

    private static string ToInternalEmail(string username)
        => username.ToLower() + "@sh.game";

    /// <summary>
    /// Đăng ký tài khoản mới qua RPC auth/register.
    /// Gọi một lần duy nhất — sau đó dùng LoginAsync để đăng nhập.
    /// </summary>
    public async Task<RegisterResponse> RegisterAsync(string username, string password)
    {
        var payload = JsonConvert.SerializeObject(new RegisterRequest
        {
            Username = username,
            Password = password
        });
        var result = await Client.RpcAsync(httpKey, "auth/register", payload);
        return JsonConvert.DeserializeObject<RegisterResponse>(result.Payload);
    }

    /// <summary>
    /// Đăng nhập bằng username + password theo quy ước email nội bộ username@sh.game.
    /// </summary>
    public async Task<ISession> LoginAsync(string username, string password)
    {
        Session = await Client.AuthenticateEmailAsync(
            ToInternalEmail(username),
            password,
            username,
            false
        );
        SaveSession(Session);
        Debug.Log($"[NakamaClient] Login success. UserId={Session.UserId}");
        return Session;
    }

    /// <summary>
    /// Gia hạn session bằng refresh token.
    /// </summary>
    public async Task RefreshSessionAsync()
    {
        Session = await Client.SessionRefreshAsync(Session);
        SaveSession(Session);
        Debug.Log($"[NakamaClient] Session refreshed. UserId={Session.UserId}");
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
    /// Cập nhật đội hình. lineup phải là mảng đúng 2 phần tử (uid hoặc null).
    /// </summary>
    public async Task<HeroSetLineupResponse> SetLineupAsync(string[] lineup)
    {
        var payload = JsonConvert.SerializeObject(new SetLineupRequest { Lineup = lineup });
        var response = await Client.RpcAsync(Session, "hero/set_lineup", payload);
        return JsonConvert.DeserializeObject<HeroSetLineupResponse>(response.Payload);
    }

    /// <summary>Nâng sao hero bằng shard. Server là nguồn sự thật — trừ shard + set rarity/star, client không tự tính.</summary>
    public async Task<HeroUpgradeResponse> UpgradeHeroAsync(int heroId)
    {
        var payload = JsonConvert.SerializeObject(new HeroUpgradeRequest { HeroId = heroId });
        var response = await Client.RpcAsync(Session, "hero/upgrade", payload);
        return JsonConvert.DeserializeObject<HeroUpgradeResponse>(response.Payload);
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
        return JsonConvert.DeserializeObject<PlayerMeResponse>(response.Payload);
    }

    public async Task<PlayerUpdateResponse> UpdatePlayerAsync(PlayerUpdateRequest request)
    {
        var payload = JsonUtility.ToJson(request);
        var response = await Client.RpcAsync(Session, "player/update", payload);
        return JsonUtility.FromJson<PlayerUpdateResponse>(response.Payload);
    }

    // ── Summon Execute ────────────────────────────────────────────────────────

    public async Task<SummonExecuteResponse> SummonHeroAsync(string optionId)
    {
        var payload  = JsonConvert.SerializeObject(new SummonRequest { OptionId = optionId });
        var response = await Client.RpcAsync(Session, "summon/hero", payload);
        return JsonConvert.DeserializeObject<SummonExecuteResponse>(response.Payload);
    }

    public async Task<SummonExecuteResponse> SummonSkillAsync(string optionId)
    {
        var payload  = JsonConvert.SerializeObject(new SummonRequest { OptionId = optionId });
        var response = await Client.RpcAsync(Session, "summon/skill", payload);
        return JsonConvert.DeserializeObject<SummonExecuteResponse>(response.Payload);
    }

    public async Task<SummonExecuteResponse> SummonWeaponAsync(string optionId)
    {
        var payload  = JsonConvert.SerializeObject(new SummonRequest { OptionId = optionId });
        var response = await Client.RpcAsync(Session, "summon/weapon", payload);
        return JsonConvert.DeserializeObject<SummonExecuteResponse>(response.Payload);
    }

    // ── Claim Reward ──────────────────────────────────────────────────────────

    public async Task<ClaimRewardResponse> SummonHeroClaimRewardAsync(int summonLevel)
    {
        var payload  = JsonConvert.SerializeObject(new ClaimRewardRequest { SummonLevel = summonLevel });
        var response = await Client.RpcAsync(Session, "summon/hero/claim_reward", payload);
        return JsonConvert.DeserializeObject<ClaimRewardResponse>(response.Payload);
    }

    public async Task<ClaimRewardResponse> SummonSkillClaimRewardAsync(int summonLevel)
    {
        var payload  = JsonConvert.SerializeObject(new ClaimRewardRequest { SummonLevel = summonLevel });
        var response = await Client.RpcAsync(Session, "summon/skill/claim_reward", payload);
        return JsonConvert.DeserializeObject<ClaimRewardResponse>(response.Payload);
    }

    public async Task<ClaimRewardResponse> SummonWeaponClaimRewardAsync(int summonLevel)
    {
        var payload  = JsonConvert.SerializeObject(new ClaimRewardRequest { SummonLevel = summonLevel });
        var response = await Client.RpcAsync(Session, "summon/weapon/claim_reward", payload);
        return JsonConvert.DeserializeObject<ClaimRewardResponse>(response.Payload);
    }

    /// <summary>Lấy trạng thái summon của cả 3 loại (gọi sau login để đồng bộ).</summary>
    public async Task<SummonStateResponse> GetSummonStateAsync()
    {
        var response = await Client.RpcAsync(Session, "summon/state", "{}");
        return JsonConvert.DeserializeObject<SummonStateResponse>(response.Payload);
    }

    // ── Skill Management ──────────────────────────────────────────────────────

    /// <summary>Lấy toàn bộ skill sở hữu, shard và trang bị (gọi sau login).</summary>
    public async Task<SkillListResponse> GetSkillListAsync()
    {
        var response = await Client.RpcAsync(Session, "skill/list", "{}");
        return JsonConvert.DeserializeObject<SkillListResponse>(response.Payload);
    }

    /// <summary>
    /// Trang bị skill vào slot của hero. Truyền skillUid = null để bỏ trang bị slot đó.
    /// </summary>
    public async Task<SkillEquipResponse> SkillEquipAsync(string heroUid, int slotIndex, string skillUid = null)
    {
        var payload = JsonConvert.SerializeObject(new SkillEquipRequest
        {
            HeroUid    = heroUid,
            SlotIndex  = slotIndex,
            SkillUid   = skillUid
        });
        var response = await Client.RpcAsync(Session, "skill/equip", payload);
        return JsonConvert.DeserializeObject<SkillEquipResponse>(response.Payload);
    }

    /// <summary>Gỡ skill khỏi bất kỳ hero/slot nào đang đeo nó.</summary>
    public async Task<SkillUnequipResponse> SkillUnequipAsync(string skillUid)
    {
        var payload = JsonConvert.SerializeObject(new SkillUnequipRequest { SkillUid = skillUid });
        var response = await Client.RpcAsync(Session, "skill/unequip", payload);
        return JsonConvert.DeserializeObject<SkillUnequipResponse>(response.Payload);
    }

    /// <summary>Nâng level toàn bộ skill bằng shard sẵn có — server tính + trừ shard + tăng
    /// level cho mọi skill đủ điều kiện trong 1 lần gọi.</summary>
    public async Task<SkillEnhanceAllResponse> EnhanceAllSkillsAsync()
    {
        var response = await Client.RpcAsync(Session, "skill/enhance_all", "{}");
        return JsonConvert.DeserializeObject<SkillEnhanceAllResponse>(response.Payload);
    }

    // ── Weapon Management ─────────────────────────────────────────────────────

    public async Task<WeaponListResponse> GetWeaponListAsync()
    {
        var response = await Client.RpcAsync(Session, "weapon/list", "{}");
        return JsonConvert.DeserializeObject<WeaponListResponse>(response.Payload);
    }

    /// <summary>Trang bị weapon vào hero (1 slot / hero). weaponUid = null để bỏ trang bị.</summary>
    public async Task<WeaponEquipResponse> WeaponEquipAsync(string heroUid, string weaponUid = null)
    {
        var payload  = JsonConvert.SerializeObject(new WeaponEquipRequest { HeroUid = heroUid, WeaponUid = weaponUid });
        var response = await Client.RpcAsync(Session, "weapon/equip", payload);
        return JsonConvert.DeserializeObject<WeaponEquipResponse>(response.Payload);
    }

    /// <summary>Gỡ weapon khỏi bất kỳ hero nào đang đeo nó.</summary>
    public async Task<WeaponUnequipResponse> WeaponUnequipAsync(string weaponUid)
    {
        var payload  = JsonConvert.SerializeObject(new WeaponUnequipRequest { WeaponUid = weaponUid });
        var response = await Client.RpcAsync(Session, "weapon/unequip", payload);
        return JsonConvert.DeserializeObject<WeaponUnequipResponse>(response.Payload);
    }

    /// <summary>Fuse vũ khí lên node kế tiếp bằng shard. Server đổi weapon_id/name/grade/star + trừ shard — client không tự tính.</summary>
    public async Task<WeaponFuseResponse> FuseWeaponAsync(int weaponId)
    {
        var payload  = JsonConvert.SerializeObject(new WeaponFuseRequest { WeaponId = weaponId });
        var response = await Client.RpcAsync(Session, "weapon/fuse", payload);
        return JsonConvert.DeserializeObject<WeaponFuseResponse>(response.Payload);
    }

    // ── Battle ────────────────────────────────────────────────────────────────

    /// <summary>Progression thật từ server (current_stage/current_chapter/highest_stage_cleared). Gọi sau login trước khi vào màn chọn stage.</summary>
    public async Task<BattleProgression> GetBattleProgressionAsync()
    {
        var response = await Client.RpcAsync(Session, "battle/progression");
        return JsonConvert.DeserializeObject<BattleProgression>(response.Payload);
    }

    /// <summary>Báo kết quả 1 trận PvE. Server tính reward + advance stage — client không tự cộng thưởng/tăng stage trước khi có response.</summary>
    public async Task<BattleEndResponse> BattleEndAsync(BattleEndRequest request)
    {
        var payload  = JsonConvert.SerializeObject(request);
        var response = await Client.RpcAsync(Session, "battle/end", payload);
        return JsonConvert.DeserializeObject<BattleEndResponse>(response.Payload);
    }

    private async void OnApplicationQuit()
    {
        await DisconnectSocketAsync();
    }
}
