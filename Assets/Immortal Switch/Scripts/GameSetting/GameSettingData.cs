using System;
using UnityEngine;

[Serializable]
public class GameSettingData
{
    [Header("Background Music")]
    public bool BackgroundMusicEnabled = true;
    [Range(0f, 1f)] public float BackgroundMusicVolume = 1f;

    [Header("SFX")]
    public bool SfxEnabled = true;
    [Range(0f, 1f)] public float SfxVolume = 1f;

    [Header("Gameplay / Visual")]
    public bool ScreenShakeEnabled = true;
    public bool DamageFontEnabled = true;
    public bool ContentNotiEnabled = true;
    public bool AutoSleepModeEnabled = false;
    public bool SkillEffectEnabled = true;
    public bool MonsterVisualEnabled = true;

    public GameSettingData Clone()
    {
        return new GameSettingData
        {
            BackgroundMusicEnabled = BackgroundMusicEnabled,
            BackgroundMusicVolume = BackgroundMusicVolume,

            SfxEnabled = SfxEnabled,
            SfxVolume = SfxVolume,

            ScreenShakeEnabled = ScreenShakeEnabled,
            DamageFontEnabled = DamageFontEnabled,
            ContentNotiEnabled = ContentNotiEnabled,
            AutoSleepModeEnabled = AutoSleepModeEnabled,
            SkillEffectEnabled = SkillEffectEnabled,
            MonsterVisualEnabled = MonsterVisualEnabled
        };
    }
}