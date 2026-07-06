using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

public class SettingView : AnimatedUIView
{
    [Header("Background Music")]
    [SerializeField] private ToggleSwitch backgroundMusicToggle;
    [SerializeField] private Slider backgroundMusicSlider;

    [Header("SFX")]
    [SerializeField] private ToggleSwitch sfxToggle;
    [SerializeField] private Slider sfxSlider;

    [Header("Gameplay / Visual")]
    [SerializeField] private ToggleSwitch screenShakeToggle;
    [SerializeField] private ToggleSwitch damageFontToggle;
    [SerializeField] private ToggleSwitch contentNotiToggle;
    [SerializeField] private ToggleSwitch autoSleepModeToggle;
    [SerializeField] private ToggleSwitch skillEffectToggle;
    [SerializeField] private ToggleSwitch monsterAppearanceToggle;


    [Header("Buttons")]
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button resetDefaultButton;

    private bool isBinding;

    private void OnEnable()
    {
        BindEvents();
        RefreshView();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    private void BindEvents()
    {
        if (backgroundMusicToggle != null)
            backgroundMusicToggle.onValueChanged.AddListener(OnBackgroundMusicToggleChanged);

        if (backgroundMusicSlider != null)
            backgroundMusicSlider.onValueChanged.AddListener(OnBackgroundMusicVolumeChanged);

        if (sfxToggle != null)
            sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

        if (screenShakeToggle != null)
            screenShakeToggle.onValueChanged.AddListener(OnScreenShakeToggleChanged);

        if (damageFontToggle != null)
            damageFontToggle.onValueChanged.AddListener(OnDamageFontToggleChanged);

        if (contentNotiToggle != null)
            contentNotiToggle.onValueChanged.AddListener(OnContentNotiToggleChanged);

        if (autoSleepModeToggle != null)
            autoSleepModeToggle.onValueChanged.AddListener(OnAutoSleepModeToggleChanged);

        if (skillEffectToggle != null)
            skillEffectToggle.onValueChanged.AddListener(OnSkillEffectToggleChanged);
        
        if (monsterAppearanceToggle != null)
            monsterAppearanceToggle.onValueChanged.AddListener(OnSkillEffectToggleChanged);

        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnLogoutButtonClicked);

        if (resetDefaultButton != null)
            resetDefaultButton.onClick.AddListener(OnResetDefaultButtonClicked);
    }

    private void UnbindEvents()
    {
        if (backgroundMusicToggle != null)
            backgroundMusicToggle.onValueChanged.RemoveListener(OnBackgroundMusicToggleChanged);

        if (backgroundMusicSlider != null)
            backgroundMusicSlider.onValueChanged.RemoveListener(OnBackgroundMusicVolumeChanged);

        if (sfxToggle != null)
            sfxToggle.onValueChanged.RemoveListener(OnSfxToggleChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);

        if (screenShakeToggle != null)
            screenShakeToggle.onValueChanged.RemoveListener(OnScreenShakeToggleChanged);

        if (damageFontToggle != null)
            damageFontToggle.onValueChanged.RemoveListener(OnDamageFontToggleChanged);

        if (contentNotiToggle != null)
            contentNotiToggle.onValueChanged.RemoveListener(OnContentNotiToggleChanged);

        if (autoSleepModeToggle != null)
            autoSleepModeToggle.onValueChanged.RemoveListener(OnAutoSleepModeToggleChanged);

        if (skillEffectToggle != null)
            skillEffectToggle.onValueChanged.RemoveListener(OnSkillEffectToggleChanged);

        if (logoutButton != null)
            logoutButton.onClick.RemoveListener(OnLogoutButtonClicked);

        if (resetDefaultButton != null)
            resetDefaultButton.onClick.RemoveListener(OnResetDefaultButtonClicked);
    }

    public void RefreshView()
    {
        if (SettingManager.Instance == null)
            return;

        isBinding = true;

        GameSettingData setting = SettingManager.Instance.CurrentSetting;

        if (backgroundMusicToggle != null)
            backgroundMusicToggle.SetIsOnWithoutNotify(setting.BackgroundMusicEnabled);

        if (backgroundMusicSlider != null)
            backgroundMusicSlider.SetValueWithoutNotify(setting.BackgroundMusicVolume);

        if (sfxToggle != null)
            sfxToggle.SetIsOnWithoutNotify(setting.SfxEnabled);

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(setting.SfxVolume);

        if (screenShakeToggle != null)
            screenShakeToggle.SetIsOnWithoutNotify(setting.ScreenShakeEnabled);

        if (damageFontToggle != null)
            damageFontToggle.SetIsOnWithoutNotify(setting.DamageFontEnabled);

        if (contentNotiToggle != null)
            contentNotiToggle.SetIsOnWithoutNotify(setting.ContentNotiEnabled);

        if (autoSleepModeToggle != null)
            autoSleepModeToggle.SetIsOnWithoutNotify(setting.AutoSleepModeEnabled);

        if (skillEffectToggle != null)
            skillEffectToggle.SetIsOnWithoutNotify(setting.SkillEffectEnabled);

        isBinding = false;
    }

    private void OnBackgroundMusicToggleChanged(bool enabled)
    {
        if (isBinding) return;
        SettingManager.Instance.SetBackgroundMusicEnabled(enabled);
    }

    private void OnBackgroundMusicVolumeChanged(float volume)
    {
        if (isBinding) return;
        SettingManager.Instance.SetBackgroundMusicVolume(volume);
    }

    private void OnSfxToggleChanged(bool enabled)
    {
        if (isBinding) return;
        SettingManager.Instance.SetSfxEnabled(enabled);
    }

    private void OnSfxVolumeChanged(float volume)
    {
        if (isBinding) return;
        SettingManager.Instance.SetSfxVolume(volume);
    }

    private void OnScreenShakeToggleChanged(bool enabled)
    {
        if (isBinding) return;
        SettingManager.Instance.SetScreenShakeEnabled(enabled);
    }

    private void OnDamageFontToggleChanged(bool enabled)
    {
        if (isBinding) return;
        SettingManager.Instance.SetDamageFontEnabled(enabled);
    }

    private void OnContentNotiToggleChanged(bool enabled)
    {
        if (isBinding) return;
        SettingManager.Instance.SetContentNotiEnabled(enabled);
    }

    private void OnAutoSleepModeToggleChanged(bool enabled)
    {
        if (isBinding) return;
        SettingManager.Instance.SetAutoSleepModeEnabled(enabled);
    }

    private void OnSkillEffectToggleChanged(bool enabled)
    {
        if (isBinding) return;
        SettingManager.Instance.SetSkillEffectEnabled(enabled);
    }
    
    private void OnMonsterVisualToggleChanged(bool enabled)
    {
        if (isBinding) return;
        SettingManager.Instance.SetSkillEffectEnabled(enabled);
    }

    private void OnLogoutButtonClicked()
    {
        SettingManager.Instance.Logout();
    }

    private void OnResetDefaultButtonClicked()
    {
        SettingManager.Instance.ResetToDefault();
        RefreshView();
    }
}