using System;
using System.Collections.Generic;
using System.Text;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using Battle;
using Common;
using Cysharp.Threading.Tasks;
using Google;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.Skill.UI;
using Immortal_Switch.Scripts.UI;
using Nakama;
using Newtonsoft.Json;
using UnityEngine;

public class SettingManager : Singleton<SettingManager>
{
    private const string SaveKey = "Game_Setting_Data";

    [Header("Runtime Setting")]
    [SerializeField]
    private GameSettingData currentSetting = new GameSettingData();

    public GameSettingData CurrentSetting => currentSetting;

    public event Action<GameSettingData> OnSettingChanged;

    public event Action<bool> OnBattleMusicToggleChanged;
    public event Action<bool> OnEventNotiToggleChanged;
    public event Action<bool> OnOffscreenToggleChanged;

    public event Action<bool> OnBackgroundMusicToggleChanged;
    public event Action<float> OnBackgroundMusicVolumeChanged;

    public event Action<bool> OnSfxToggleChanged;
    public event Action<float> OnSfxVolumeChanged;

    public event Action<bool> OnScreenShakeChanged;
    public event Action<bool> OnDamageFontChanged;
    public event Action<bool> OnContentNotiChanged;
    public event Action<bool> OnAutoSleepModeChanged;
    public event Action<bool> OnSkillEffectChanged;
    public event Action<bool> OnMonsterVisualChanged;

    protected override void Awake()
    {
        base.Awake();
        LoadSetting();
        ApplyAllSettings();
    }

    private void Update()
    {
        // Bơm callback native của Sign In with Apple vào main thread — bắt buộc theo AppleAuth plugin.
        _appleAuthManager?.Update();
    }

    public override UniTask InitializeAsync()
    {
        throw new NotImplementedException();
    }

#region Load / Save

    public void LoadSetting()
    {
        if (ES3.KeyExists(SaveKey))
        {
            currentSetting = ES3.Load<GameSettingData>(SaveKey);
        }
        else
        {
            currentSetting = new GameSettingData();
            SaveSetting();
        }

        ClampSettingValues();
    }

    public void SaveSetting()
    {
        ClampSettingValues();
        ES3.Save(SaveKey, currentSetting);
        Debug.Log($"SaveSetting: {JsonConvert.SerializeObject(currentSetting)}");
    }

    public void ResetToDefault()
    {
        currentSetting = new GameSettingData();
        SaveSetting();
        ApplyAllSettings();
        NotifySettingChanged();
    }

    private void ClampSettingValues()
    {
        currentSetting.BackgroundMusicVolume = Mathf.Clamp01(currentSetting.BackgroundMusicVolume);
        currentSetting.SfxVolume = Mathf.Clamp01(currentSetting.SfxVolume);
    }

#endregion

    public void SetFpsValue(int value)
    {
        if (currentSetting.Fps == value)
            return;

        currentSetting.Fps = value;
        SaveSetting();
        NotifySettingChanged();
    }

    public void SetLangCode(string langCode)
    {
        if (currentSetting.LangCode == langCode)
            return;

        currentSetting.LangCode = langCode;
        SaveSetting();

        ApplyLangCode(langCode);
        NotifySettingChanged();
    }

    public void SetEventNotiEnabled(bool enabled)
    {
        if (currentSetting.EventNotiEnabled == enabled)
            return;

        currentSetting.EventNotiEnabled = enabled;
        SaveSetting();

        ApplyEventNotiEnabled(enabled);
        OnEventNotiToggleChanged?.Invoke(enabled);
        NotifySettingChanged();
    }

    public void SetBattleMusicEnabled(bool enabled)
    {
        if (currentSetting.BattleMusicEnabled == enabled)
            return;

        currentSetting.BattleMusicEnabled = enabled;
        SaveSetting();

        ApplyBattleMusicEnabled(enabled);
        OnBattleMusicToggleChanged?.Invoke(enabled);
        NotifySettingChanged();
    }

    public void SetOffscreenTimeIdx(int idx)
    {
        if (currentSetting.OffscreenIdx == idx)
            return;

        currentSetting.OffscreenIdx = idx;
        SaveSetting();
        ApplyOffscreenEnabled(enabled);
        OnOffscreenToggleChanged?.Invoke(enabled);
        NotifySettingChanged();
    }

    public void SetGraphicIdx(int idx)
    {
        if (currentSetting.GraphicIdx == idx)
            return;

        currentSetting.GraphicIdx = idx;
        SaveSetting();
        NotifySettingChanged();
    }

    public void SetQualityIdx(int idx)
    {
        if (currentSetting.QualityIdx == idx)
            return;

        currentSetting.QualityIdx = idx;
        SaveSetting();
        NotifySettingChanged();
    }

    public void SetOffscreenEnabled(bool enabled)
    {
        if (currentSetting.OffscreenEnabled == enabled)
            return;

        currentSetting.OffscreenEnabled = enabled;
        SaveSetting();

        ApplyOffscreenEnabled(enabled);
        OnOffscreenToggleChanged?.Invoke(enabled);
        NotifySettingChanged();
    }

#region Background Music

    public void SetBackgroundMusicEnabled(bool enabled)
    {
        if (currentSetting.BackgroundMusicEnabled == enabled)
            return;

        currentSetting.BackgroundMusicEnabled = enabled;
        SaveSetting();

        ApplyBackgroundMusicEnabled(enabled);
        OnBackgroundMusicToggleChanged?.Invoke(enabled);
        NotifySettingChanged();
    }

    public void SetBackgroundMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (Mathf.Approximately(currentSetting.BackgroundMusicVolume, volume))
            return;

        currentSetting.BackgroundMusicVolume = volume;
        SaveSetting();

        ApplyBackgroundMusicVolume(volume);
        OnBackgroundMusicVolumeChanged?.Invoke(volume);
        NotifySettingChanged();
    }

    public bool IsBackgroundMusicEnabled()
    {
        return currentSetting.BackgroundMusicEnabled;
    }

    public float GetBackgroundMusicVolume()
    {
        return currentSetting.BackgroundMusicVolume;
    }

#endregion

#region SFX

    public void SetSfxEnabled(bool enabled)
    {
        if (currentSetting.SfxEnabled == enabled)
            return;

        currentSetting.SfxEnabled = enabled;
        SaveSetting();

        ApplySfxEnabled(enabled);
        OnSfxToggleChanged?.Invoke(enabled);
        NotifySettingChanged();
    }

    public void SetSfxVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (Mathf.Approximately(currentSetting.SfxVolume, volume))
            return;

        currentSetting.SfxVolume = volume;
        SaveSetting();

        ApplySfxVolume(volume);
        OnSfxVolumeChanged?.Invoke(volume);
        NotifySettingChanged();
    }

    public bool IsSfxEnabled()
    {
        return currentSetting.SfxEnabled;
    }

    public float GetSfxVolume()
    {
        return currentSetting.SfxVolume;
    }

#endregion

#region Screen Shake

    public void SetScreenShakeEnabled(bool enabled)
    {
        if (currentSetting.ScreenShakeEnabled == enabled)
            return;

        currentSetting.ScreenShakeEnabled = enabled;
        SaveSetting();

        ApplyScreenShakeEnabled(enabled);
        OnScreenShakeChanged?.Invoke(enabled);
        NotifySettingChanged();
    }

    public bool IsScreenShakeEnabled()
    {
        return currentSetting.ScreenShakeEnabled;
    }

#endregion

#region Damage Font

    public void SetDamageFontEnabled(bool enabled)
    {
        if (currentSetting.DamageFontEnabled == enabled)
            return;

        currentSetting.DamageFontEnabled = enabled;
        SaveSetting();

        ApplyDamageFontEnabled(enabled);
        OnDamageFontChanged?.Invoke(enabled);
        NotifySettingChanged();
    }

    public bool IsDamageFontEnabled()
    {
        return currentSetting.DamageFontEnabled;
    }

#endregion

#region Content Notification

    public void SetContentNotiEnabled(bool enabled)
    {
        if (currentSetting.ContentNotiEnabled == enabled)
            return;

        currentSetting.ContentNotiEnabled = enabled;
        SaveSetting();

        ApplyContentNotiEnabled(enabled);
        OnContentNotiChanged?.Invoke(enabled);
        NotifySettingChanged();
    }

    public bool IsContentNotiEnabled()
    {
        return currentSetting.ContentNotiEnabled;
    }

#endregion

#region Auto Sleep Mode

    public void SetAutoSleepModeEnabled(bool enabled)
    {
        if (currentSetting.AutoSleepModeEnabled == enabled)
            return;

        currentSetting.AutoSleepModeEnabled = enabled;
        SaveSetting();

        ApplyAutoSleepModeEnabled(enabled);
        OnAutoSleepModeChanged?.Invoke(enabled);
        NotifySettingChanged();
    }

    public bool IsAutoSleepModeEnabled()
    {
        return currentSetting.AutoSleepModeEnabled;
    }

#endregion

#region Skill Effect

    public void SetSkillEffectEnabled(bool enabled)
    {
        if (currentSetting.SkillEffectEnabled == enabled)
            return;

        currentSetting.SkillEffectEnabled = enabled;
        SaveSetting();

        ApplySkillEffectEnabled(enabled);
        OnSkillEffectChanged?.Invoke(enabled);
        NotifySettingChanged();
    }

    public void SetMonsterVisualEnabled(bool enabled)
    {
        if (currentSetting.MonsterVisualEnabled == enabled)
            return;

        currentSetting.MonsterVisualEnabled = enabled;
        SaveSetting();

        ApplySkillEffectEnabled(enabled);
        OnMonsterVisualChanged?.Invoke(enabled);
        NotifySettingChanged();
    }

    public bool IsSkillEffectEnabled()
    {
        return currentSetting.SkillEffectEnabled;
    }

#endregion

#region Logout

    public void Logout()
    {
        LogoutAsync().Forget();
    }

    private async UniTask LogoutAsync()
    {
        await UIManager.Instance.DespawnAllSessionViewsAsync();

        // Đợi request hero/set_lineup (nếu vừa swap hero giữa trận) ghi xong lên server trước
        // khi dọn battle session — CleanupBattle huỷ session ngay, không chờ request nào đang
        // chạy nền, nên logout ngay sau khi swap có thể làm mất thay đổi lineup vừa rồi.
        if (BattleHeroSessionController.Instance != null)
            await BattleHeroSessionController.Instance.FlushPendingLineupSyncAsync();

        // Tương tự cho skill equip/unequip/replace/auto-equip vừa thao tác — các request này cũng
        // fire-and-forget nên logout ngay sau khi đổi skill có thể làm mất thay đổi vừa rồi.
        if (SkillViewDataProvider.Instance != null)
            await SkillViewDataProvider.Instance.FlushPendingSkillSyncAsync();

        //await Transitioner.Instance.TransitionOutWithoutChangingScene(destroyCancellationToken);
        PvEBattleController.Instance.CleanupBattle(true);
        DatabaseManager.Instance.ReleaseGameDatabase();
        await UniTask.Yield();

        //Transitioner.Instance.TransitionInWithoutChangingScene();
        await NakamaClient.Instance.HandleForceLogout("Nothing");
    }

#endregion

#region Delete Account

    public void DeleteAccount()
    {
        DeleteAccountAsync().Forget();
    }

    private async UniTask DeleteAccountAsync()
    {
        try
        {
            await NakamaClient.Instance.DeleteAccountAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SettingManager] DeleteAccount failed: {e.Message}");
            UIManager.Instance.ShowToast("Xoá tài khoản thất bại. Vui lòng thử lại.");
            return;
        }

        await UIManager.Instance.DespawnAllSessionViewsAsync();

        PvEBattleController.Instance.CleanupBattle(true);
        await UniTask.Yield();

        await NakamaClient.Instance.HandleForceLogout("Tài khoản đã được xoá.");
    }

#endregion

#region Account Link

    // Chỉ account guest (device) hoặc BD (username/password qua auth/register) mới link được —
    // server enforce qua beforeLinkGoogle/beforeLinkApple (nakama/src/handler/account.js). Android
    // link Google, iOS link Apple; không hỗ trợ nền tảng khác.
    [Header("Google Sign-In")]
    [SerializeField]
    private string googleWebClientId = "546099158752-8bgak6biutovg9ke6qavt2aktstihbdk.apps.googleusercontent.com";

    private IAppleAuthManager _appleAuthManager;

    public bool IsAccountLinked => UserDataCache.Instance.IsSocialLinked;

    public async UniTask<bool> LinkAccountAsync()
    {
        if (IsAccountLinked)
        {
            UIManager.Instance.ShowToast("Tài khoản đã được liên kết.");
            return false;
        }

        try
        {
            string provider;
            string token;

            if (Application.platform == RuntimePlatform.Android)
            {
                provider = "Google";
                token = await GetGoogleIdTokenAsync();
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                provider = "Apple";
                token = await GetAppleIdentityTokenAsync();
            }
            else
            {
                UIManager.Instance.ShowToast("Liên kết tài khoản chỉ hỗ trợ trên thiết bị Android/iOS.");
                return false;
            }

            if (provider == "Google")
            {
                await NakamaClient.Instance.LinkGoogleAsync(token);
                UserDataCache.Instance.GoogleLinked = true;
            }
            else
            {
                await NakamaClient.Instance.LinkAppleAsync(token);
                UserDataCache.Instance.AppleLinked = true;
            }

            UIManager.Instance.ShowToast($"Liên kết {provider} thành công.");
            return true;
        }
        catch (OperationCanceledException)
        {
            // Người dùng huỷ Google/Apple sign-in — không hiện toast lỗi.
            return false;
        }
        catch (ApiResponseException e)
        {
            Debug.LogError($"[SettingManager] LinkAccount failed: {e.StatusCode} {e.Message}");
            UIManager.Instance.ShowToast(e.StatusCode == 409
                ? "Tài khoản này đã được liên kết với một tài khoản khác."
                : "Liên kết tài khoản thất bại. Vui lòng thử lại.");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SettingManager] LinkAccount failed: {e.Message}");
            UIManager.Instance.ShowToast("Liên kết tài khoản thất bại. Vui lòng thử lại.");
            return false;
        }
    }

    /// <summary>Nhận thưởng 1 lần cho account đã link Google/Apple qua RPC account/claim_link_reward.
    /// Trả về true nếu nhận thành công (đã cập nhật UserDataCache.LinkRewardClaimed + diamond).</summary>
    public async UniTask<bool> ClaimLinkRewardAsync()
    {
        if (!IsAccountLinked)
        {
            UIManager.Instance.ShowToast("Bạn cần liên kết tài khoản trước khi nhận thưởng.");
            return false;
        }

        if (UserDataCache.Instance.LinkRewardClaimed)
        {
            UIManager.Instance.ShowToast("Bạn đã nhận thưởng liên kết rồi.");
            return false;
        }

        try
        {
            var response = await NakamaClient.Instance.ClaimLinkRewardAsync();
            UserDataCache.Instance.LinkRewardClaimed = true;
            CurrencyManager.Instance.Set(CurrencyType.diamond, response.gems);

            if (response.rewards != null)
            {
                var itemRewards = new List<ItemData>();
                foreach (var r in response.rewards)
                {
                    if (r.ItemId > 0 && r.Amount > 0)
                        itemRewards.Add(new ItemData(r.ItemId, r.Amount));
                }

                if (itemRewards.Count > 0)
                    PopupRewardService.Show(itemRewards);
            }

            return true;
        }
        catch (ApiResponseException e)
        {
            Debug.LogError($"[SettingManager] ClaimLinkReward failed: {e.StatusCode} {e.Message}");
            UIManager.Instance.ShowToast("Nhận thưởng thất bại. Vui lòng thử lại.");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SettingManager] ClaimLinkReward failed: {e.Message}");
            UIManager.Instance.ShowToast("Nhận thưởng thất bại. Vui lòng thử lại.");
            return false;
        }
    }

    private async UniTask<string> GetGoogleIdTokenAsync()
    {
        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            WebClientId = googleWebClientId,
            RequestIdToken = true,
            UseGameSignIn = false
        };

        var user = await GoogleSignIn.DefaultInstance.SignIn();
        if (user == null)
            throw new OperationCanceledException("Người dùng huỷ đăng nhập Google.");

        return user.IdToken;
    }

    private async UniTask<string> GetAppleIdentityTokenAsync()
    {
        if (_appleAuthManager == null && AppleAuthManager.IsCurrentPlatformSupported)
            _appleAuthManager = new AppleAuthManager(new PayloadDeserializer());

        if (_appleAuthManager == null)
            throw new NotSupportedException("Sign In with Apple không được hỗ trợ trên thiết bị này.");

        var tcs = new UniTaskCompletionSource<string>();
        _appleAuthManager.LoginWithAppleId(
            new AppleAuthLoginArgs(LoginOptions.IncludeEmail),
            credential =>
            {
                var identityToken = (credential as IAppleIDCredential)?.IdentityToken;
                if (identityToken == null)
                    tcs.TrySetException(new Exception("Không lấy được identity token từ Apple."));
                else
                    tcs.TrySetResult(Encoding.UTF8.GetString(identityToken));
            },
            error => tcs.TrySetException(new Exception($"Sign In with Apple thất bại: {error.GetAuthorizationErrorCode()}"))
        );
        return await tcs.Task;
    }

#endregion

#region Apply Logic Placeholder

    private void ApplyAllSettings()
    {
        ApplyBattleMusicEnabled(currentSetting.BattleMusicEnabled);
        ApplyEventNotiEnabled(currentSetting.EventNotiEnabled);
        ApplyOffscreenEnabled(currentSetting.OffscreenEnabled);

        ApplyBackgroundMusicEnabled(currentSetting.BackgroundMusicEnabled);
        ApplyBackgroundMusicVolume(currentSetting.BackgroundMusicVolume);

        ApplySfxEnabled(currentSetting.SfxEnabled);
        ApplySfxVolume(currentSetting.SfxVolume);

        ApplyScreenShakeEnabled(currentSetting.ScreenShakeEnabled);
        ApplyDamageFontEnabled(currentSetting.DamageFontEnabled);
        ApplyContentNotiEnabled(currentSetting.ContentNotiEnabled);
        ApplyAutoSleepModeEnabled(currentSetting.AutoSleepModeEnabled);
        ApplySkillEffectEnabled(currentSetting.SkillEffectEnabled);

        NotifySettingChanged();
    }

    private void ApplyLangCode(string langCode)
    {
        LocalizationManager.SetLanguage(langCode);
    }

    private void ApplyEventNotiEnabled(bool enabled)
    {
        // TODO:
        // Sau này gắn AudioManager ở đây.
        // Ví dụ:
        // AudioManager.Instance.SetBgmEnabled(enabled);
    }

    private void ApplyBattleMusicEnabled(bool enabled)
    {
        // TODO:
        // Sau này gắn AudioManager ở đây.
        // Ví dụ:
        // AudioManager.Instance.SetBgmEnabled(enabled);
    }

    private void ApplyBackgroundMusicEnabled(bool enabled)
    {
        // TODO:
        // Sau này gắn AudioManager ở đây.
        // Ví dụ:
        // AudioManager.Instance.SetBgmEnabled(enabled);
    }

    private void ApplyOffscreenEnabled(bool enabled)
    {
        // TODO:
        // Sau này gắn AudioManager ở đây.
        // Ví dụ:
        // AudioManager.Instance.SetBgmEnabled(enabled);
    }

    private void ApplyBackgroundMusicVolume(float volume)
    {
        // TODO:
        // Sau này gắn AudioManager ở đây.
        // Ví dụ:
        // AudioManager.Instance.SetBgmVolume(volume);
    }

    private void ApplySfxEnabled(bool enabled)
    {
        // TODO:
        // Sau này gắn AudioManager ở đây.
        // Ví dụ:
        // AudioManager.Instance.SetSfxEnabled(enabled);
    }

    private void ApplySfxVolume(float volume)
    {
        // TODO:
        // Sau này gắn AudioManager ở đây.
        // Ví dụ:
        // AudioManager.Instance.SetSfxVolume(volume);
    }

    private void ApplyScreenShakeEnabled(bool enabled)
    {
        // TODO:
        // Sau này gắn CameraShakeManager hoặc CinemachineShake ở đây.
        // Ví dụ:
        // CameraShakeManager.Instance.SetEnabled(enabled);
    }

    private void ApplyDamageFontEnabled(bool enabled)
    {
        // TODO:
        // Sau này gắn DamageNumberManager ở đây.
        // Ví dụ:
        // DamageNumberManager.Instance.SetVisible(enabled);
    }

    private void ApplyContentNotiEnabled(bool enabled)
    {
        // TODO:
        // Sau này gắn NotificationManager ở đây.
        // Ví dụ:
        // ContentNotificationManager.Instance.SetEnabled(enabled);
    }

    private void ApplyAutoSleepModeEnabled(bool enabled)
    {
        // TODO:
        // Sau này gắn AutoSleepManager ở đây.
        // Ví dụ:
        // AutoSleepManager.Instance.SetEnabled(enabled);
    }

    private void ApplySkillEffectEnabled(bool enabled)
    {
        // TODO:
        // Sau này gắn SkillEffectManager / SpawnSkillObject filter ở đây.
        // Ví dụ:
        // SkillEffectManager.Instance.SetEffectVisible(enabled);
    }

    private void ApplyMonsterVisualEnabled(bool enabled)
    {
        // TODO:
        // Sau này gắn SkillEffectManager / SpawnSkillObject filter ở đây.
        // Ví dụ:
        // SkillEffectManager.Instance.SetEffectVisible(enabled);
    }

    private void NotifySettingChanged()
    {
        OnSettingChanged?.Invoke(currentSetting.Clone());
    }

#endregion
}