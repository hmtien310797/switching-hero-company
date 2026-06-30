using System;
using System.Collections;
using System.Threading.Tasks;
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
    private void HandleForceLogout(string reason)
    {
        if (_forceLogoutHandled) return;
        _forceLogoutHandled = true;

        Debug.LogWarning($"[NakamaClient] Force logout: {reason}");
        ClearSession();
        LastForceLogoutReason = reason;
        ForceLoggedOut?.Invoke(reason);
        _ = ReturnToLoginSceneAsync();
    }

    private async Task ReturnToLoginSceneAsync()
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

    private async void OnApplicationQuit()
    {
        await DisconnectSocketAsync();
    }
}
