using System;
using Battle;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

public class SettingManager : Singleton<SettingManager>
{
    private const string SaveKey = "Game_Setting_Data";

    [Header("Runtime Setting")]
    [SerializeField] private GameSettingData currentSetting = new GameSettingData();

    public GameSettingData CurrentSetting => currentSetting;

    public event Action<GameSettingData> OnSettingChanged;

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
        // TODO:
        // 1. Stop battle / pause gameplay if needed.
        // 2. Save current user data.
        // 3. Clear local session token.
        // 4. Disconnect socket / backend session.
        // 5. Return to login scene.
        // 6. Clear runtime cache if needed.
        LogoutAsync().Forget();
        Debug.Log("[SettingManager] Logout called. This is placeholder logic.");
    }

    private async UniTask LogoutAsync()
    {
        UIManager.Instance.TogglePopupAsync<TopMainView>();
        UIManager.Instance.TogglePopupAsync<BottomMainView>();
        UIManager.Instance.TogglePopupAsync<SettingView>();
        //await Transitioner.Instance.TransitionOutWithoutChangingScene(destroyCancellationToken);
        PvEBattleController.Instance.CleanupBattle(true);
        await UniTask.Yield();
        //Transitioner.Instance.TransitionInWithoutChangingScene();
        await NakamaClient.Instance.HandleForceLogout("Nothing");
    }

    #endregion

    #region Apply Logic Placeholder

    private void ApplyAllSettings()
    {
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

    private void ApplyBackgroundMusicEnabled(bool enabled)
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