using System;
using Immortal_Switch.Scripts.Shared.Constants;
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
    public bool OffscreenEnabled = true;
    public int OffscreenIdx = -1;
    
    public bool ScreenShakeEnabled = true;
    public bool DamageFontEnabled = true;
    public bool ContentNotiEnabled = true;
    public bool AutoSleepModeEnabled = false;
    public bool SkillEffectEnabled = true;
    public bool MonsterVisualEnabled = true;
    public bool BattleMusicEnabled = true;
    
    public bool EventNotiEnabled = true;
    public string LangCode = ValueConstants.DEFAULT_LANGUAGE;

    public int GraphicIdx = 0;
    public int QualityIdx = 0;
    public int Fps = ValueConstants.MAX_FPS;

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
            MonsterVisualEnabled = MonsterVisualEnabled,
            BattleMusicEnabled = BattleMusicEnabled,
            GraphicIdx = GraphicIdx,
            QualityIdx = QualityIdx,
            Fps = Fps,
            EventNotiEnabled = EventNotiEnabled,
            OffscreenEnabled = OffscreenEnabled,
            LangCode = LangCode,
        };
    }
}