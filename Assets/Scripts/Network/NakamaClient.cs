using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Nakama;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NakamaClient : MonoBehaviour
{
    private const string SessionPrefKey        = "nakama.session";
    private const string RefreshTokenPrefKey   = "nakama.refreshtoken";
    private const string SingletonName         = "[NakamaClient]";
    private const string LoginSceneName        = "LoginScene";

    /// <summary>Tần suất kiểm tra session còn hạn hay không (giây). Chạy song song với
    /// auto-refresh nội bộ của Nakama SDK — SDK refresh âm thầm và không báo lỗi ra ngoài,
    /// nên watchdog này là nơi duy nhất phát hiện refresh token đã hết hạn (vd: AFK quá lâu)
    /// và đưa người chơi về LoginScene thay vì để app bị treo, không thao tác được nữa.</summary>
    private const float SessionWatchdogIntervalSec = 30f;

    /// <summary>Chủ động refresh khi AuthToken còn cách hạn dưới ngần này (giây), để không phải
    /// chờ tới khi nó hết hạn hẳn rồi mới phát hiện refresh token cũng đã chết.</summary>
    private const int SessionRefreshLeadSec = 120;

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

    /// <summary>
    /// Bắn ra khi server buộc đăng xuất tài khoản hiện tại (vd: cùng tài khoản vừa đăng nhập
    /// ở thiết bị khác). Session local đã bị xoá trước khi event này bắn.
    /// </summary>
    public event Action<string> ForceLoggedOut;

    /// <summary>Lý do của lần force-logout gần nhất — LoginScene đọc giá trị này sau khi bị
    /// chuyển scene để hiển thị thông báo cho người dùng, rồi nên tự xoá.</summary>
    public string LastForceLogoutReason { get; set; }

    private bool _forceLogoutHandled;
    private bool _intentionalSocketClose;

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
        Socket.Closed += (reason) =>
        {
            Debug.Log($"[NakamaClient] Socket closed: {reason}");
            var wasIntentional = _intentionalSocketClose;
            _intentionalSocketClose = false;
            if (!wasIntentional)
                HandleForceLogout("Mất kết nối với server — có thể tài khoản đã đăng nhập ở thiết bị khác.");
        };
        Socket.ReceivedError += ex => Debug.LogError($"[NakamaClient] Socket error: {ex.Message}");

        _ = TryRestoreSessionAsync();
        StartCoroutine(SessionWatchdogRoutine());
    }

    /// <summary>
    /// Định kỳ kiểm tra AuthToken sắp hết hạn và chủ động refresh. Đây là cơ chế dự phòng cho
    /// auto-refresh nội bộ của Nakama SDK: nếu refresh token cũng đã hết hạn (vd: app mở AFK quá
    /// 1 tiếng không thao tác), SDK chỉ log lỗi rồi thôi chứ không báo cho code game biết — khiến
    /// app treo, không gọi RPC nào được nữa. Watchdog này gọi RefreshSessionAsync() (đã có sẵn
    /// try/catch bắt 401 → HandleForceLogout) nên lỗi sẽ được phát hiện và đưa người chơi về
    /// LoginScene thay vì im lặng treo máy.
    /// </summary>
    private IEnumerator SessionWatchdogRoutine()
    {
        var wait = new WaitForSeconds(SessionWatchdogIntervalSec);
        while (true)
        {
            yield return wait;
            if (Session == null) continue;
            if (!Session.HasExpired(DateTime.UtcNow.AddSeconds(SessionRefreshLeadSec))) continue;

            _ = TryRefreshSessionSilentlyAsync();
        }
    }

    /// <summary>Gọi RefreshSessionAsync() và nuốt exception — HandleForceLogout (nếu cần) đã
    /// chạy bên trong RefreshSessionAsync trước khi nó rethrow, nên ở đây không cần xử lý thêm.</summary>
    private async Task TryRefreshSessionSilentlyAsync()
    {
        try
        {
            await RefreshSessionAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[NakamaClient] Watchdog refresh failed: {e.Message}");
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // App vừa quay lại foreground sau một khoảng thời gian bị suspend (timer nội bộ của SDK
        // không chạy khi app bị pause) — kiểm tra lại session ngay thay vì chờ tới chu kỳ watchdog.
        if (!pauseStatus && Session != null && Session.HasExpired(DateTime.UtcNow.AddSeconds(SessionRefreshLeadSec)))
        {
            _ = TryRefreshSessionSilentlyAsync();
        }
    }

    private void SaveSession(ISession session)
    {
        _forceLogoutHandled = false;
        PlayerPrefs.SetString(SessionPrefKey, session.AuthToken);
        PlayerPrefs.SetString(RefreshTokenPrefKey, session.RefreshToken ?? "");
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Server đã invalidate session hiện tại (sessionLogout/sessionDisconnect khi login ở thiết bị
    /// khác, hoặc socket bị đóng ngoài ý muốn). Xoá session local, bắn event và quay về LoginScene.
    /// </summary>
    public async UniTask HandleForceLogout(string reason)
    {
        if (_forceLogoutHandled) return;
        _forceLogoutHandled = true;

        Debug.LogWarning($"[NakamaClient] Force logout: {reason}");
        ClearSession();
        LastForceLogoutReason = reason;
        ForceLoggedOut?.Invoke(reason);
        await ReturnToLoginSceneAsync();
        GameEventManager.Trigger(GameEvents.OnUserLogOut);
    }

    private async UniTask ReturnToLoginSceneAsync()
    {
        await DisconnectSocketAsync();
        if (SceneManager.GetActiveScene().name != LoginSceneName)
            await SceneManager.LoadSceneAsync(LoginSceneName);
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
    /// Link tài khoản Google vào account đang đăng nhập (Android). googleToken là idToken lấy từ
    /// Google Sign-In SDK. Server chặn qua beforeLinkGoogle nếu account không phải guest/BD (đã
    /// link Google/Apple từ trước) — xem nakama/src/handler/account.js.
    /// </summary>
    public async Task LinkGoogleAsync(string googleToken)
    {
        await Client.LinkGoogleAsync(Session, googleToken);
        Debug.Log($"[NakamaClient] Google linked. UserId={Session.UserId}");
    }

    /// <summary>
    /// Link tài khoản Apple vào account đang đăng nhập (iOS). appleToken là identityToken lấy từ
    /// Sign In with Apple SDK. Cùng rule chặn với LinkGoogleAsync.
    /// </summary>
    public async Task LinkAppleAsync(string appleToken)
    {
        await Client.LinkAppleAsync(Session, appleToken);
        Debug.Log($"[NakamaClient] Apple linked. UserId={Session.UserId}");
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
        try
        {
            Session = await Client.SessionRefreshAsync(Session);
            SaveSession(Session);
            Debug.Log($"[NakamaClient] Session refreshed. UserId={Session.UserId}");
        }
        catch (ApiResponseException e) when (e.StatusCode == 401)
        {
            HandleForceLogout("Tài khoản đã đăng nhập ở thiết bị khác.");
            throw;
        }
    }

    /// <summary>
    /// Gọi RPC đã authenticate, tự phát hiện trường hợp session bị server invalidate (401 —
    /// thường do tài khoản vừa đăng nhập ở thiết bị khác) và chuyển về LoginScene.
    /// </summary>
    private async Task<IApiRpc> CallRpcAsync(string id, string payload = null)
    {
        try
        {
            return payload == null
                ? await Client.RpcAsync(Session, id)
                : await Client.RpcAsync(Session, id, payload);
        }
        catch (ApiResponseException e) when (e.StatusCode == 401)
        {
            HandleForceLogout("Tài khoản đã đăng nhập ở thiết bị khác.");
            throw;
        }
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
        if (!IsSocketConnected) return;
        _intentionalSocketClose = true;
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
        var response = await CallRpcAsync("hero/list");
        return JsonConvert.DeserializeObject<HeroListResponse>(response.Payload);
    }

    /// <summary>
    /// Cập nhật đội hình. lineup phải là mảng đúng 2 phần tử (uid hoặc null).
    /// </summary>
    public async Task<HeroSetLineupResponse> SetLineupAsync(string[] lineup)
    {
        var payload = JsonConvert.SerializeObject(new SetLineupRequest { Lineup = lineup });
        var response = await CallRpcAsync("hero/set_lineup", payload);
        return JsonConvert.DeserializeObject<HeroSetLineupResponse>(response.Payload);
    }

    /// <summary>Nâng sao hero bằng shard. Server là nguồn sự thật — trừ shard + set rarity/star, client không tự tính.</summary>
    public async Task<HeroUpgradeResponse> UpgradeHeroAsync(int heroId)
    {
        var payload = JsonConvert.SerializeObject(new HeroUpgradeRequest { HeroId = heroId });
        var response = await CallRpcAsync("hero/upgrade", payload);
        return JsonConvert.DeserializeObject<HeroUpgradeResponse>(response.Payload);
    }

    // ── Bag ───────────────────────────────────────────────────────────────────

    public async Task<BagResponse> GetBagAsync()
    {
        var response = await CallRpcAsync("bag/get");
        return JsonConvert.DeserializeObject<BagResponse>(response.Payload);
    }

    // ── Player ────────────────────────────────────────────────────────────────

    public async Task<PlayerMeResponse> GetPlayerMeAsync()
    {
        var response = await CallRpcAsync("player/me");
        return JsonConvert.DeserializeObject<PlayerMeResponse>(response.Payload);
    }

    public async Task<PlayerUpdateResponse> UpdatePlayerAsync(PlayerUpdateRequest request)
    {
        var payload = JsonUtility.ToJson(request);
        var response = await CallRpcAsync("player/update", payload);
        return JsonUtility.FromJson<PlayerUpdateResponse>(response.Payload);
    }

    /// <summary>Đổi display_name qua RPC player/rename. Server validate độ dài 2-20 ký tự.</summary>
    public async Task<PlayerRenameResponse> RenamePlayerAsync(string displayName)
    {
        var payload = JsonUtility.ToJson(new PlayerRenameRequest { display_name = displayName });
        var response = await CallRpcAsync("player/rename", payload);
        return JsonUtility.FromJson<PlayerRenameResponse>(response.Payload);
    }

    /// <summary>Xoá vĩnh viễn tài khoản qua RPC account/delete. Không thể hoàn tác.</summary>
    public async Task DeleteAccountAsync()
    {
        await CallRpcAsync("account/delete", "{}");
    }

    /// <summary>Nhận thưởng liên kết Google/Apple (1 lần) qua RPC account/claim_link_reward.
    /// Server từ chối nếu account chưa link hoặc đã nhận rồi.</summary>
    public async Task<AccountClaimLinkRewardResponse> ClaimLinkRewardAsync()
    {
        var response = await CallRpcAsync("account/claim_link_reward", "{}");
        return JsonConvert.DeserializeObject<AccountClaimLinkRewardResponse>(response.Payload);
    }

    // ── Summon Execute ────────────────────────────────────────────────────────

    public async Task<SummonExecuteResponse> SummonHeroAsync(string optionId)
    {
        var payload  = JsonConvert.SerializeObject(new SummonRequest { OptionId = optionId });
        var response = await CallRpcAsync("summon/hero", payload);
        return JsonConvert.DeserializeObject<SummonExecuteResponse>(response.Payload);
    }

    public async Task<SummonExecuteResponse> SummonSkillAsync(string optionId)
    {
        var payload  = JsonConvert.SerializeObject(new SummonRequest { OptionId = optionId });
        var response = await CallRpcAsync("summon/skill", payload);
        return JsonConvert.DeserializeObject<SummonExecuteResponse>(response.Payload);
    }

    public async Task<SummonExecuteResponse> SummonWeaponAsync(string optionId)
    {
        var payload  = JsonConvert.SerializeObject(new SummonRequest { OptionId = optionId });
        var response = await CallRpcAsync("summon/weapon", payload);
        return JsonConvert.DeserializeObject<SummonExecuteResponse>(response.Payload);
    }

    // ── Claim Reward ──────────────────────────────────────────────────────────

    public async Task<ClaimRewardResponse> SummonHeroClaimRewardAsync(int summonLevel)
    {
        var payload  = JsonConvert.SerializeObject(new ClaimRewardRequest { SummonLevel = summonLevel });
        var response = await CallRpcAsync("summon/hero/claim_reward", payload);
        return JsonConvert.DeserializeObject<ClaimRewardResponse>(response.Payload);
    }

    public async Task<ClaimRewardResponse> SummonSkillClaimRewardAsync(int summonLevel)
    {
        var payload  = JsonConvert.SerializeObject(new ClaimRewardRequest { SummonLevel = summonLevel });
        var response = await CallRpcAsync("summon/skill/claim_reward", payload);
        return JsonConvert.DeserializeObject<ClaimRewardResponse>(response.Payload);
    }

    public async Task<ClaimRewardResponse> SummonWeaponClaimRewardAsync(int summonLevel)
    {
        var payload  = JsonConvert.SerializeObject(new ClaimRewardRequest { SummonLevel = summonLevel });
        var response = await CallRpcAsync("summon/weapon/claim_reward", payload);
        return JsonConvert.DeserializeObject<ClaimRewardResponse>(response.Payload);
    }

    /// <summary>Lấy trạng thái summon của cả 3 loại (gọi sau login để đồng bộ).</summary>
    public async Task<SummonStateResponse> GetSummonStateAsync()
    {
        var response = await CallRpcAsync("summon/state", "{}");
        return JsonConvert.DeserializeObject<SummonStateResponse>(response.Payload);
    }

    // ── Skill Management ──────────────────────────────────────────────────────

    /// <summary>Lấy toàn bộ skill sở hữu, shard và trang bị (gọi sau login).</summary>
    public async Task<SkillListResponse> GetSkillListAsync()
    {
        var response = await CallRpcAsync("skill/list", "{}");
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
        var response = await CallRpcAsync("skill/equip", payload);
        return JsonConvert.DeserializeObject<SkillEquipResponse>(response.Payload);
    }

    /// <summary>Trang bị nhiều skill cho 1 hero trong 1 lần gọi duy nhất (server tự gỡ skill cũ
    /// không còn trong danh sách + tự chọn slot trống) — tránh bắn nhiều RPC equip/unequip rời rạc
    /// và đồng thời, vốn có thể ghi đè lẫn nhau trên server.</summary>
    public async Task<SkillAutoEquipResponse> SkillAutoEquipAsync(string heroUid, IEnumerable<string> skillUids)
    {
        var payload = JsonConvert.SerializeObject(new SkillAutoEquipRequest
        {
            HeroUid   = heroUid,
            SkillUids = skillUids != null ? skillUids.ToArray() : new string[0]
        });
        var response = await CallRpcAsync("skill/auto_equip", payload);
        return JsonConvert.DeserializeObject<SkillAutoEquipResponse>(response.Payload);
    }

    /// <summary>Gỡ skill khỏi bất kỳ hero/slot nào đang đeo nó.</summary>
    public async Task<SkillUnequipResponse> SkillUnequipAsync(string skillUid)
    {
        var payload = JsonConvert.SerializeObject(new SkillUnequipRequest { SkillUid = skillUid });
        var response = await CallRpcAsync("skill/unequip", payload);
        return JsonConvert.DeserializeObject<SkillUnequipResponse>(response.Payload);
    }

    /// <summary>Nâng level toàn bộ skill bằng shard sẵn có — server tính + trừ shard + tăng
    /// level cho mọi skill đủ điều kiện trong 1 lần gọi.</summary>
    public async Task<SkillEnhanceAllResponse> EnhanceAllSkillsAsync()
    {
        var response = await CallRpcAsync("skill/enhance_all", "{}");
        return JsonConvert.DeserializeObject<SkillEnhanceAllResponse>(response.Payload);
    }

    // ── Weapon Management ─────────────────────────────────────────────────────

    public async Task<WeaponListResponse> GetWeaponListAsync()
    {
        var response = await CallRpcAsync("weapon/list", "{}");
        return JsonConvert.DeserializeObject<WeaponListResponse>(response.Payload);
    }

    /// <summary>Trang bị weapon vào hero (id-based — xem Docs/be-weapon-equip-upgrade-rpc-spec.md mục 3). weaponId bỏ qua khi category = "exclusive".</summary>
    public async Task<WeaponEquipResponse> EquipWeaponAsync(int heroId, string category, int weaponId = 0)
    {
        var payload  = JsonConvert.SerializeObject(new WeaponEquipRequest { HeroId = heroId, Category = category, WeaponId = weaponId });
        var response = await CallRpcAsync("weapon/equip", payload);
        return JsonConvert.DeserializeObject<WeaponEquipResponse>(response.Payload);
    }

    /// <summary>Level up weapon (xem Docs/be-weapon-equip-upgrade-rpc-spec.md mục 4). heroId dùng khi category = "exclusive", weaponId dùng khi "standard".</summary>
    public async Task<WeaponUpgradeResponse> UpgradeWeaponAsync(string category, int weaponId = 0, int heroId = 0)
    {
        var payload  = JsonConvert.SerializeObject(new WeaponUpgradeRequest { Category = category, WeaponId = weaponId, HeroId = heroId });
        var response = await CallRpcAsync("weapon/upgrade", payload);
        return JsonConvert.DeserializeObject<WeaponUpgradeResponse>(response.Payload);
    }

    /// <summary>Limit break weapon (xem Docs/be-weapon-equip-upgrade-rpc-spec.md mục 5). heroId dùng khi category = "exclusive", weaponId dùng khi "standard".</summary>
    public async Task<WeaponLimitBreakResponse> LimitBreakWeaponAsync(string category, int weaponId = 0, int heroId = 0)
    {
        var payload  = JsonConvert.SerializeObject(new WeaponLimitBreakRequest { Category = category, WeaponId = weaponId, HeroId = heroId });
        var response = await CallRpcAsync("weapon/limitbreak", payload);
        return JsonConvert.DeserializeObject<WeaponLimitBreakResponse>(response.Payload);
    }

    /// <summary>Gỡ weapon khỏi bất kỳ hero nào đang đeo nó.</summary>
    public async Task<WeaponUnequipResponse> WeaponUnequipAsync(string weaponUid)
    {
        var payload  = JsonConvert.SerializeObject(new WeaponUnequipRequest { WeaponUid = weaponUid });
        var response = await CallRpcAsync("weapon/unequip", payload);
        return JsonConvert.DeserializeObject<WeaponUnequipResponse>(response.Payload);
    }

    /// <summary>Fuse vũ khí lên node kế tiếp bằng shard. Server đổi weapon_id/name/grade/star + trừ shard — client không tự tính.</summary>
    public async Task<WeaponFuseResponse> FuseWeaponAsync(int weaponId)
    {
        var payload  = JsonConvert.SerializeObject(new WeaponFuseRequest { WeaponId = weaponId });
        var response = await CallRpcAsync("weapon/fuse", payload);
        return JsonConvert.DeserializeObject<WeaponFuseResponse>(response.Payload);
    }

    // ── Growth Management ─────────────────────────────────────────────────────
    // Xem Docs/be-growth-rpc-spec.md.

    /// <summary>Đồng bộ toàn bộ tiến trình Growth (current_unlocked_tier + stack từng stat) — gọi sau login và mỗi lần mở màn Growth.</summary>
    public async Task<GrowthStateResponse> GetGrowthStateAsync()
    {
        var response = await CallRpcAsync("growth/state", "{}");
        return JsonConvert.DeserializeObject<GrowthStateResponse>(response.Payload);
    }

    /// <summary>Mua thêm stack cho 1 stat bằng Gold. Server tự clamp theo trần stack còn lại — trả bought_amount thực tế.</summary>
    public async Task<GrowthUpgradeResponse> UpgradeGrowthAsync(string stat, int amount)
    {
        var payload  = JsonConvert.SerializeObject(new GrowthUpgradeRequest { Stat = stat, Amount = amount });
        var response = await CallRpcAsync("growth/upgrade", payload);
        return JsonConvert.DeserializeObject<GrowthUpgradeResponse>(response.Payload);
    }

    /// <summary>Mở tier kế tiếp (miễn phí — chỉ là gate xác nhận). Server tự suy next_tier = current_unlocked_tier + 1.</summary>
    public async Task<GrowthUnlockTierResponse> UnlockGrowthTierAsync()
    {
        var response = await CallRpcAsync("growth/unlocktier", "{}");
        return JsonConvert.DeserializeObject<GrowthUnlockTierResponse>(response.Payload);
    }

    // ── IAP ───────────────────────────────────────────────────────────────────

    /// <summary>Gửi pack_id + receipt (Apple/Google, gỡ ra từ unified receipt của Unity IAP) lên
    /// RPC iap/purchase — server tự verify với store tương ứng RỒI cộng thưởng theo pack_diamond
    /// config. KHÔNG dùng Client.ValidatePurchaseApple/GoogleAsync (API built-in của Nakama) vì nó
    /// chỉ verify + chống replay, không biết pack_id nên không cộng được gì cho người chơi.</summary>
    public async Task<IapPurchaseResponse> IapPurchaseAsync(int packId, string store, string receipt)
    {
        var payload = JsonConvert.SerializeObject(new IapPurchaseRequest
        {
            PackId  = packId,
            Store   = store,
            Receipt = receipt,
        });
        var response = await CallRpcAsync("iap/purchase", payload);
        return JsonConvert.DeserializeObject<IapPurchaseResponse>(response.Payload);
    }

    /// <summary>Gửi pack_id + receipt lên RPC iap/pack_purchase — dùng cho pack_iap (bundle nhiều
    /// item, vd. gói Special), khác iap/purchase chỉ cộng 1 loại tiền theo pack_diamond. Server tự
    /// verify receipt rồi cộng cả 3 slot item + enforce purchase limit theo pack.limit_reset.</summary>
    public async Task<IapPackPurchaseResponse> IapPackPurchaseAsync(int packId, string store, string receipt)
    {
        var payload = JsonConvert.SerializeObject(new IapPackPurchaseRequest
        {
            PackId  = packId,
            Store   = store,
            Receipt = receipt,
        });
        var response = await CallRpcAsync("iap/pack_purchase", payload);
        return JsonConvert.DeserializeObject<IapPackPurchaseResponse>(response.Payload);
    }

    // ── Recharge Milestone ────────────────────────────────────────────────────

    /// <summary>Điểm tích nạp tháng hiện tại + trạng thái claimed từng mốc — nguồn sự thật, gọi
    /// khi login và mỗi lần mở màn Shop/GloryPass (xem ShopManager.SyncRechargeStateAsync).</summary>
    public async Task<RechargeStateResponse> RechargeStateAsync()
    {
        var response = await CallRpcAsync("recharge/state", "{}");
        return JsonConvert.DeserializeObject<RechargeStateResponse>(response.Payload);
    }

    /// <summary>Nhận thưởng 1 mốc tích nạp (game_recharge_milestone) — server tự kiểm tra số lượt
    /// tích nạp trong tháng hiện tại (cộng +1 mỗi lần iap/purchase hoặc iap/pack_purchase thành
    /// công) trước khi cộng thưởng; throw nếu chưa đủ điểm hoặc mốc đã nhận trong tháng này.</summary>
    public async Task<RechargeClaimResponse> RechargeClaimAsync(int milestoneId)
    {
        var payload  = JsonConvert.SerializeObject(new RechargeClaimRequest { MilestoneId = milestoneId });
        var response = await CallRpcAsync("recharge/claim", payload);
        return JsonConvert.DeserializeObject<RechargeClaimResponse>(response.Payload);
    }

    // ── Monthly Pass ──────────────────────────────────────────────────────────
    // Xem handler/monthly_pass.js — pack_iap các row pack_type="subscription" (Monthly Pass tab).
    // Mua vẫn đi qua IapPackPurchaseAsync (pack_iap); 2 RPC này chỉ phụ trách trạng thái/nhận
    // thưởng ngày (server tự tính ngày từ purchased_at, client không tự đếm ngày cục bộ nữa).

    /// <summary>Trạng thái mua/nhận thưởng của mọi Monthly Pass — nguồn sự thật, gọi khi login và
    /// mỗi lần mở màn Shop/MonthlyPass (xem ShopManager.SyncMonthlyPassStateAsync).</summary>
    public async Task<MonthlyPassStateResponse> MonthlyPassStateAsync()
    {
        var response = await CallRpcAsync("monthlypass/state", "{}");
        return JsonConvert.DeserializeObject<MonthlyPassStateResponse>(response.Payload);
    }

    /// <summary>Nhận thưởng ngày hiện tại của 1 Monthly Pass đã mua. Server tự suy ngày từ
    /// purchased_at và throw nếu chưa mua/đã hết hạn/ngày đó đã nhận rồi.</summary>
    public async Task<MonthlyPassClaimResponse> MonthlyPassClaimAsync(int packId)
    {
        var payload  = JsonConvert.SerializeObject(new MonthlyPassClaimRequest { PackId = packId });
        var response = await CallRpcAsync("monthlypass/claim", payload);
        return JsonConvert.DeserializeObject<MonthlyPassClaimResponse>(response.Payload);
    }

    // ── Event Wheel (Lucky Wheel) ────────────────────────────────────────────
    // Xem handler/event_wheel.js. Premium (paid) pass track chưa có RPC mua — server chưa có
    // pack_iap row cho nó, xem comment đầu file server.

    /// <summary>Snapshot đầy đủ: reward pool, shop, pass 100 mốc, số dư vé/point. Gọi khi mở
    /// EventWheelView và sau mỗi lần spin/shop_buy/pass_claim thành công để đồng bộ lại.</summary>
    public async Task<EventWheelStateResponse> GetEventWheelStateAsync()
    {
        var response = await CallRpcAsync("eventwheel/state", "{}");
        return JsonConvert.DeserializeObject<EventWheelStateResponse>(response.Payload);
    }

    /// <summary>Quay 1 hoặc 10 lượt (category: 1=normal, 2=premium). Server trừ vé, random có
    /// trọng số, trả về đúng ô (slot_index) mà client phải dừng animation tại đó.</summary>
    public async Task<EventWheelSpinResponse> EventWheelSpinAsync(EventWheelSpinRequest request)
    {
        var payload  = JsonConvert.SerializeObject(request);
        var response = await CallRpcAsync("eventwheel/spin", payload);
        return JsonConvert.DeserializeObject<EventWheelSpinResponse>(response.Payload);
    }

    /// <summary>Mua 1 item trong shop sự kiện bằng lucky_wheel_point.</summary>
    public async Task<EventWheelShopBuyResponse> EventWheelShopBuyAsync(int shopSlotId)
    {
        var payload  = JsonConvert.SerializeObject(new EventWheelShopBuyRequest { ShopSlotId = shopSlotId });
        var response = await CallRpcAsync("eventwheel/shop_buy", payload);
        return JsonConvert.DeserializeObject<EventWheelShopBuyResponse>(response.Payload);
    }

    /// <summary>Nhận thưởng của 1 mốc pass khi đã đủ spin_required. track: "free" (mặc định) |
    /// "paid" — "paid" cần đã mua Premium Pass (xem EventWheelPassBuyPremiumAsync).</summary>
    public async Task<EventWheelPassClaimResponse> EventWheelPassClaimAsync(int level, string track = "free")
    {
        var payload  = JsonConvert.SerializeObject(new EventWheelPassClaimRequest { Level = level, Track = track });
        var response = await CallRpcAsync("eventwheel/pass_claim", payload);
        return JsonConvert.DeserializeObject<EventWheelPassClaimResponse>(response.Payload);
    }

    /// <summary>Validate receipt IAP và mở khoá track paid của pass sự kiện đang active — không tự
    /// cộng thưởng, EventWheelPassClaimAsync(level, "paid") mới cộng paid_item từng mốc.</summary>
    public async Task<EventWheelPassBuyPremiumResponse> EventWheelPassBuyPremiumAsync(string store, string receipt)
    {
        var payload  = JsonConvert.SerializeObject(new EventWheelPassBuyPremiumRequest { Store = store, Receipt = receipt });
        var response = await CallRpcAsync("eventwheel/pass_buy_premium", payload);
        return JsonConvert.DeserializeObject<EventWheelPassBuyPremiumResponse>(response.Payload);
    }

    // ── Event Lễ Hội Băng Long ────────────────────────────────────────────────
    // Xem handler/event_bl.js. Server là nguồn sự thật duy nhất (thay cho
    // EventLeHoiBangLongService/Storage ES3 cục bộ trước đây — xem EventLeHoiBangLongManager).

    /// <summary>Snapshot đầy đủ: 7 ngày check-in, bonus theo ngày, nhiệm vụ + mốc điểm nhiệm vụ,
    /// mốc điểm triệu hồi, bảng tỉ lệ gacha. Gọi khi mở EventLeHoiBangLongView và sau mỗi
    /// claim/summon thành công để đồng bộ lại.</summary>
    public async Task<EventBLStateResponse> GetEventBLStateAsync()
    {
        var response = await CallRpcAsync("eventbl/state", "{}");
        return JsonConvert.DeserializeObject<EventBLStateResponse>(response.Payload);
    }

    /// <summary>Báo tiến độ nhiệm vụ theo trigger (LOGIN được server tự cộng khi sang ngày mới;
    /// các trigger còn lại — CLEAR_STAGE_COUNT/HERO_SUMMON/ENHANCE_GEAR/KILL_MONSTER/CLAIM_IDLE —
    /// do client báo khi gameplay event tương ứng xảy ra). Server tự kẹp theo target và bỏ qua
    /// nhiệm vụ đã claim, không tự tin tưởng điểm/thưởng — chỉ tiến độ.</summary>
    public async Task<EventBLMissionProgressResponse> EventBLMissionProgressAsync(string trigger, int value)
    {
        var payload  = JsonConvert.SerializeObject(new EventBLMissionProgressRequest { Trigger = trigger, Value = value });
        var response = await CallRpcAsync("eventbl/mission_progress", payload);
        return JsonConvert.DeserializeObject<EventBLMissionProgressResponse>(response.Payload);
    }

    /// <summary>Nhận thưởng đăng nhập miễn phí ngày N (game_event_BL_check_in.js).</summary>
    public async Task<EventBLClaimLoginResponse> EventBLClaimLoginAsync(int day)
    {
        var payload  = JsonConvert.SerializeObject(new EventBLClaimLoginRequest { Day = day });
        var response = await CallRpcAsync("eventbl/claim_login", payload);
        return JsonConvert.DeserializeObject<EventBLClaimLoginResponse>(response.Payload);
    }

    /// <summary>Nhận track bonus miễn phí ("instant") của ngày N — mở khoá nút mua track trả phí.</summary>
    public async Task<EventBLClaimBonusFreeResponse> EventBLClaimBonusFreeAsync(int day)
    {
        var payload  = JsonConvert.SerializeObject(new EventBLClaimBonusFreeRequest { Day = day });
        var response = await CallRpcAsync("eventbl/claim_bonus_free", payload);
        return JsonConvert.DeserializeObject<EventBLClaimBonusFreeResponse>(response.Payload);
    }

    /// <summary>Đánh dấu đã mua track bonus trả phí ngày N — chỉ ghi trạng thái, vật phẩm đã được
    /// cấp thật lúc iap/pack_purchase thành công (xem EventBLCheckInBonusDto).</summary>
    public async Task<EventBLDayResponse> EventBLConfirmBonusPurchaseAsync(int day)
    {
        var payload  = JsonConvert.SerializeObject(new EventBLDayRequest { Day = day });
        var response = await CallRpcAsync("eventbl/confirm_bonus_purchase", payload);
        return JsonConvert.DeserializeObject<EventBLDayResponse>(response.Payload);
    }

    /// <summary>Đánh dấu đã nhận track bonus trả phí ngày N — chỉ ghi trạng thái, xem trên.</summary>
    public async Task<EventBLDayResponse> EventBLClaimBonusPaidAsync(int day)
    {
        var payload  = JsonConvert.SerializeObject(new EventBLDayRequest { Day = day });
        var response = await CallRpcAsync("eventbl/claim_bonus_paid", payload);
        return JsonConvert.DeserializeObject<EventBLDayResponse>(response.Payload);
    }

    /// <summary>Nhận thưởng 1 nhiệm vụ ngày đã hoàn thành. Điểm/thưởng luôn lấy từ config server.</summary>
    public async Task<EventBLClaimMissionResponse> EventBLClaimMissionAsync(string missionId)
    {
        var payload  = JsonConvert.SerializeObject(new EventBLClaimMissionRequest { MissionId = missionId });
        var response = await CallRpcAsync("eventbl/claim_mission", payload);
        return JsonConvert.DeserializeObject<EventBLClaimMissionResponse>(response.Payload);
    }

    /// <summary>Nhận 1 mốc điểm nhiệm vụ tích luỹ.</summary>
    public async Task<EventBLMilestoneResponse> EventBLClaimMissionMilestoneAsync(int milestone)
    {
        var payload  = JsonConvert.SerializeObject(new EventBLMilestoneRequest { Milestone = milestone });
        var response = await CallRpcAsync("eventbl/claim_mission_milestone", payload);
        return JsonConvert.DeserializeObject<EventBLMilestoneResponse>(response.Payload);
    }

    /// <summary>Nhận 1 mốc điểm triệu hồi tích luỹ.</summary>
    public async Task<EventBLMilestoneResponse> EventBLClaimSummonMilestoneAsync(int milestone)
    {
        var payload  = JsonConvert.SerializeObject(new EventBLMilestoneRequest { Milestone = milestone });
        var response = await CallRpcAsync("eventbl/claim_summon_milestone", payload);
        return JsonConvert.DeserializeObject<EventBLMilestoneResponse>(response.Payload);
    }

    /// <summary>Quay Băng Long Summon 1 hoặc 10 lượt. Trừ 1x summon_ticket_hero_banner (item 47)
    /// mỗi lượt (xem EventBLSummonResponse). Server random có trọng số theo game_event_BL_rate.js
    /// và trả đúng phần thưởng đã cấp.</summary>
    public async Task<EventBLSummonResponse> EventBLSummonAsync(int times)
    {
        var payload  = JsonConvert.SerializeObject(new EventBLSummonRequest { Times = times });
        var response = await CallRpcAsync("eventbl/summon", payload);
        return JsonConvert.DeserializeObject<EventBLSummonResponse>(response.Payload);
    }

    // ── Battle ────────────────────────────────────────────────────────────────

    /// <summary>Progression thật từ server (current_stage/current_chapter/highest_stage_cleared). Gọi sau login trước khi vào màn chọn stage.</summary>
    public async Task<BattleProgression> GetBattleProgressionAsync()
    {
        var response = await CallRpcAsync("battle/progression");
        return JsonConvert.DeserializeObject<BattleProgression>(response.Payload);
    }

    /// <summary>Báo kết quả 1 trận PvE. Server tính reward + advance stage — client không tự cộng thưởng/tăng stage trước khi có response.</summary>
    public async Task<BattleEndResponse> BattleEndAsync(BattleEndRequest request)
    {
        var payload  = JsonConvert.SerializeObject(request);
        var response = await CallRpcAsync("battle/end", payload);
        return JsonConvert.DeserializeObject<BattleEndResponse>(response.Payload);
    }

    /// <summary>Báo "đã giết hết creep của CurrentStage" — gọi ngay trước khi boss xuất hiện, để server nhớ giúp resume thẳng vào boss nếu app đóng/crash trước khi đánh xong.</summary>
    public async Task<BattleCheckpointResponse> BattleCheckpointAsync(BattleCheckpointRequest request)
    {
        var payload  = JsonConvert.SerializeObject(request);
        var response = await CallRpcAsync("battle/checkpoint", payload);
        return JsonConvert.DeserializeObject<BattleCheckpointResponse>(response.Payload);
    }

    // ── Leaderboard ───────────────────────────────────────────────────────────
    // Xem handler/leaderboard.js — bảng xếp hạng theo highest_stage_cleared, dùng leaderboard
    // built-in của Nakama. season_end_at (từ GetLeaderboardStageTopAsync) và thưởng cuối mùa
    // (GetLeaderboardSeasonRewardStateAsync/ClaimLeaderboardSeasonRewardAsync) đọc từ
    // game_config_leaderboard*.xlsx.

    /// <summary>Top bảng xếp hạng stage (mặc định 100 người đầu). cursor để phân trang tiếp — truyền lại next_cursor của lần gọi trước.</summary>
    public async Task<LeaderboardStageTopResponse> GetLeaderboardStageTopAsync(int limit = 100, string cursor = null)
    {
        var payload  = JsonConvert.SerializeObject(new { limit, cursor });
        var response = await CallRpcAsync("leaderboard/stage/top", payload);
        return JsonConvert.DeserializeObject<LeaderboardStageTopResponse>(response.Payload);
    }

    /// <summary>Các record xếp hạng gần với chính người chơi hiện tại (nk.leaderboardRecordsHaystack) — dùng để hiển thị "hạng của tôi".</summary>
    public async Task<LeaderboardStageAroundMeResponse> GetLeaderboardStageAroundMeAsync(int limit = 5)
    {
        var payload  = JsonConvert.SerializeObject(new { limit });
        var response = await CallRpcAsync("leaderboard/stage/around_me", payload);
        return JsonConvert.DeserializeObject<LeaderboardStageAroundMeResponse>(response.Payload);
    }

    /// <summary>Gọi khi mở màn Leaderboard — có thưởng mùa trước chưa nhận (has_reward=true) thì hiện banner/claim. Qua expire_at thì server tự cộng vào bag ở lần player/me kế tiếp, không mất thưởng.</summary>
    public async Task<LeaderboardSeasonRewardStateResponse> GetLeaderboardSeasonRewardStateAsync()
    {
        var response = await CallRpcAsync("leaderboard/season_reward/state", "{}");
        return JsonConvert.DeserializeObject<LeaderboardSeasonRewardStateResponse>(response.Payload);
    }

    /// <summary>Nhận thưởng mùa trước (nếu còn trong hạn) — cộng thẳng vào bag, trả updated_resources để áp qua CurrencyManager.</summary>
    public async Task<LeaderboardSeasonRewardClaimResponse> ClaimLeaderboardSeasonRewardAsync()
    {
        var response = await CallRpcAsync("leaderboard/season_reward/claim", "{}");
        return JsonConvert.DeserializeObject<LeaderboardSeasonRewardClaimResponse>(response.Payload);
    }

    // ── Dungeon ───────────────────────────────────────────────────────────────

    /// <summary>Progression thật từ server (highest_stage_cleared/next_stage mỗi dungeon) + ticket_balance. Gọi khi mở màn Dungeon.</summary>
    public async Task<DungeonStateResponse> GetDungeonStateAsync()
    {
        var response = await CallRpcAsync("dungeon/state");
        return JsonConvert.DeserializeObject<DungeonStateResponse>(response.Payload);
    }

    /// <summary>Báo kết quả 1 trận Dungeon. Server trừ vé + tính reward + cập nhật highest_stage_cleared — client không tự trừ vé/cộng thưởng.</summary>
    public async Task<DungeonEndResponse> DungeonEndAsync(DungeonEndRequest request)
    {
        var payload  = JsonConvert.SerializeObject(request);
        var response = await CallRpcAsync("dungeon/end", payload);
        return JsonConvert.DeserializeObject<DungeonEndResponse>(response.Payload);
    }

    /// <summary>Bảng reward baked sẵn theo từng stage của 1 dungeon — dùng cho preview khi duyệt Next/Prev, thay cho tính formula local.</summary>
    public async Task<DungeonStageTableResponse> GetDungeonStageTableAsync(string dungeonKey)
    {
        var payload  = JsonConvert.SerializeObject(new { dungeon_key = dungeonKey });
        var response = await CallRpcAsync("dungeon/stage_table", payload);
        return JsonConvert.DeserializeObject<DungeonStageTableResponse>(response.Payload);
    }

    // ── Transmutation ─────────────────────────────────────────────────────────

    /// <summary>Đồng bộ toàn bộ state Luyện Hóa (level/exp/energy/equips/pending) — gọi sau login và mỗi lần mở màn Luyện Hóa.</summary>
    public async Task<TransmutationListResponse> GetTransmutationListAsync()
    {
        var response = await CallRpcAsync("transmutation/list", "{}");
        return JsonConvert.DeserializeObject<TransmutationListResponse>(response.Payload);
    }

    /// <summary>Roll 1 lần. RNG luôn nằm server-side — không có field nào để gửi lên (xem Docs/be-transmutation-rpc-spec.md mục 4).</summary>
    public async Task<TransmutationFuseResponse> FuseTransmutationAsync()
    {
        var response = await CallRpcAsync("transmutation/fuse", "{}");
        return JsonConvert.DeserializeObject<TransmutationFuseResponse>(response.Payload);
    }

    /// <summary>Chốt giữ item pending hiện tại — server tự biết pending của user, không cần gửi id.</summary>
    public async Task<TransmutationEquipResponse> EquipTransmutationAsync()
    {
        var response = await CallRpcAsync("transmutation/equip", "{}");
        return JsonConvert.DeserializeObject<TransmutationEquipResponse>(response.Payload);
    }

    /// <summary>Huỷ item pending hiện tại.</summary>
    public async Task<TransmutationDismantleResponse> DismantleTransmutationAsync()
    {
        var response = await CallRpcAsync("transmutation/dismantle", "{}");
        return JsonConvert.DeserializeObject<TransmutationDismantleResponse>(response.Payload);
    }

    /// <summary>Lưu cấu hình auto-fuse lên server. Gọi khi bật/tắt auto, bắt đầu session, hoặc auto tự dừng.</summary>
    public async Task<TransmutationSaveSettingResponse> SaveTransmutationSettingAsync(
        TransmutationSaveSettingRequest request)
    {
        var payload  = JsonConvert.SerializeObject(request);
        var response = await CallRpcAsync("transmutation/setting/save", payload);
        return JsonConvert.DeserializeObject<TransmutationSaveSettingResponse>(response.Payload);
    }

    // ── AFK Reward ────────────────────────────────────────────────────────────

    /// <summary>
    /// Lưu checkpoint AFK lên server khi app pause/quit hoặc stage thay đổi.
    /// Server ghi server_now và afkStage làm mốc tính offline elapsed.
    /// </summary>
    public async Task<AfkCheckpointResponse> SaveAfkCheckpointAsync(int afkStage)
    {
        var payload  = JsonConvert.SerializeObject(new AfkCheckpointRequest { AfkStage = afkStage });
        var response = await CallRpcAsync("afk/checkpoint", payload);
        return JsonConvert.DeserializeObject<AfkCheckpointResponse>(response.Payload);
    }

    /// <summary>
    /// Claim offline AFK reward khi app resume. Server tự đọc checkpoint đã lưu,
    /// tính elapsed (server time), cộng reward và trả balances.
    /// </summary>
    public async Task<AfkClaimResponse> ClaimAfkRewardAsync()
    {
        var response = await CallRpcAsync("afk/claim", "{}");
        return JsonConvert.DeserializeObject<AfkClaimResponse>(response.Payload);
    }

    /// <summary>
    /// Xem trước reward sẽ nhận nếu claim ngay bây giờ — KHÔNG ghi checkpoint, KHÔNG cộng bag.
    /// Dùng để hiển thị popup claim với số liệu thật trước khi player bấm Claim (response
    /// không có field balances vì chưa có gì được cộng).
    /// </summary>
    public async Task<AfkClaimResponse> PeekAfkRewardAsync()
    {
        var response = await CallRpcAsync("afk/preview", "{}");
        return JsonConvert.DeserializeObject<AfkClaimResponse>(response.Payload);
    }

    // ── Mission ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy trạng thái nhiệm vụ từ server. Luôn gửi kèm initialStateJson để server tự khởi tạo
    /// nếu player chưa có state (tránh 2 round-trips). Server bỏ qua nếu đã có state.
    /// </summary>
    public async Task<MissionStateResponse> GetMissionStateAsync(string initialStateJson)
    {
        string payload = string.IsNullOrEmpty(initialStateJson)
            ? "{}"
            : "{\"initial_state\":" + initialStateJson + "}";
        var response = await CallRpcAsync("mission/state", payload);
        return JsonConvert.DeserializeObject<MissionStateResponse>(response.Payload);
    }

    /// <summary>
    /// Push toàn bộ MissionSystemData lên server (fire-and-forget). Server giữ reset dates riêng.
    /// </summary>
    public async Task SyncMissionStateAsync(string dataJson)
    {
        await CallRpcAsync("mission/sync", "{\"state\":" + dataJson + "}");
    }

    /// <summary>
    /// Nhận thưởng nhiệm vụ cụ thể. Server mark claimed và cộng vào bag, trả balances.
    /// </summary>
    public async Task<MissionClaimResponse> ClaimMissionAsync(MissionClaimRequest request)
    {
        var payload  = JsonConvert.SerializeObject(request);
        var response = await CallRpcAsync("mission/claim", payload);
        return JsonConvert.DeserializeObject<MissionClaimResponse>(response.Payload);
    }

    /// <summary>
    /// Nhận thưởng mốc điểm (DAILY/WEEKLY). Client gửi BASE rewards (chưa x2);
    /// server tự nhân đôi khi is_ads_x2=true.
    /// </summary>
    public async Task<MissionClaimResponse> ClaimMissionGroupAsync(MissionClaimGroupRequest request)
    {
        var payload  = JsonConvert.SerializeObject(request);
        var response = await CallRpcAsync("mission/claim_group", payload);
        return JsonConvert.DeserializeObject<MissionClaimResponse>(response.Payload);
    }

    // ── Tutorial ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy danh sách stepId đã hoàn thành từ server — dùng để đối soát với ES3 local
    /// (TutorialStorage) khi lệch giữa các thiết bị (vd cài lại app).
    /// </summary>
    public async Task<TutorialStateResponse> GetTutorialStateAsync()
    {
        var response = await CallRpcAsync("tutorial/state", "{}");
        return JsonConvert.DeserializeObject<TutorialStateResponse>(response.Payload);
    }

    /// <summary>
    /// Báo hoàn thành 1 bước tutorial. Server ghi progress + cấp thưởng (nếu bước có
    /// rewardItems) và trả balances tuyệt đối — idempotent, gọi lại step đã xong không
    /// cấp thưởng lần 2.
    /// </summary>
    public async Task<TutorialCompleteStepResponse> CompleteTutorialStepAsync(int stepId)
    {
        var payload  = JsonConvert.SerializeObject(new TutorialCompleteStepRequest { StepId = stepId });
        var response = await CallRpcAsync("tutorial/complete_step", payload);
        return JsonConvert.DeserializeObject<TutorialCompleteStepResponse>(response.Payload);
    }

    private async void OnApplicationQuit()
    {
        await DisconnectSocketAsync();
    }
}
