using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.Shared.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GameSetting.Views.Layouts
{
    public class UISettingAccountLayout : MonoBehaviour
    {
        [Header("Graphic references")]
        [SerializeField]
        private Slider sliderFps;

        [SerializeField]
        private TextMeshProUGUI txtFpsValue;

        [SerializeField]
        private UIToggleGroup tgGraphic;

        [SerializeField]
        private UIToggleGroup tgQuality;

        [Header("Quality references")]
        [SerializeField]
        private UIToggle toggleBgMusic;

        [SerializeField]
        private UIToggle toggleOffscreen;

        [SerializeField]
        private UIToggle toggleBattleMusic;

        [SerializeField]
        private UIToggle toggleScreenShake;

        [SerializeField]
        private UIToggle toggleDamageFont;

        [SerializeField]
        private UIToggle toggleContentNoty;

        [SerializeField]
        private UIToggle toggleSkillEffect;

        [SerializeField]
        private UIToggle toggleMonsterAppearance;

        [Header("Volume references")]
        [SerializeField]
        private Slider sliderSfx;

        [SerializeField]
        private TextMeshProUGUI txtVolumeValue;

        [SerializeField]
        private Slider sliderBgm;

        [SerializeField]
        private TextMeshProUGUI txtBgmValue;

        private void Awake()
        {
            RefreshMax();
            RefreshCurrent();
        }

        private void OnEnable()
        {
            BindEvents();
        }

        private void OnDisable()
        {
            UnBindEvents();
        }

        private void RefreshCurrent()
        {
            var current = SettingManager.Instance.CurrentSetting;
            toggleOffscreen.Bind(OnOffscreenToggleChanged, current.OffscreenEnabled);
            toggleBgMusic.Bind(OnBgMusicToggleChanged, current.BackgroundMusicEnabled);
            toggleBattleMusic.Bind(OnBattleMusicToggleChanged, current.BattleMusicEnabled);
            toggleScreenShake.Bind(OnScreenShakeToggleChanged, current.ScreenShakeEnabled);
            toggleDamageFont.Bind(OnDamageFontToggleChanged, current.DamageFontEnabled);
            toggleContentNoty.Bind(OnContentNotyToggleChanged, current.ContentNotiEnabled);
            toggleSkillEffect.Bind(OnSkillEffectToggleChanged, current.SkillEffectEnabled);
            toggleMonsterAppearance.Bind(OnMonsterAppearanceToggleChanged, current.MonsterVisualEnabled);

            tgGraphic.Bind(OnGraphicValueChanged, current.GraphicIdx);
            tgQuality.Bind(OnQualityValueChanged, current.QualityIdx);

            txtFpsValue.text = $"{current.Fps}";
            sliderFps.SetValueWithoutNotify(current.Fps);

            txtVolumeValue.text = $"{Mathf.RoundToInt(current.SfxVolume * ValueConstants.MAX_SFX)}";
            sliderSfx.SetValueWithoutNotify(current.SfxVolume * ValueConstants.MAX_SFX);

            txtBgmValue.text = $"{Mathf.RoundToInt(current.BackgroundMusicVolume * ValueConstants.MAX_BGM)}";
            sliderBgm.SetValueWithoutNotify(current.BackgroundMusicVolume * ValueConstants.MAX_BGM);
        }

        private void OnGraphicValueChanged(bool arg1, int arg2)
        {
            if (arg1)
            {
                SettingManager.Instance.SetGraphicIdx(arg2);
            }
        }

        private void OnQualityValueChanged(bool arg1, int arg2)
        {
            if (arg1)
            {
                SettingManager.Instance.SetQualityIdx(arg2);
            }
        }

        private void RefreshMax()
        {
            sliderFps.maxValue = ValueConstants.MAX_FPS;
            sliderSfx.maxValue = ValueConstants.MAX_SFX;
            sliderBgm.maxValue = ValueConstants.MAX_BGM;
        }

        private void BindEvents()
        {
            sliderFps.onValueChanged.AddListener(OnSliderFpsValueChanged);
            sliderBgm.onValueChanged.AddListener(OnSliderBgmValueChanged);
            sliderSfx.onValueChanged.AddListener(OnSliderSfxValueChanged);
        }

        private void OnSliderFpsValueChanged(float arg0)
        {
            var fps = Mathf.RoundToInt(arg0);
            txtFpsValue.text = $"{fps}";
            Application.targetFrameRate = fps;
            SettingManager.Instance.SetFpsValue(fps);
        }

        private void OnSliderSfxValueChanged(float arg0)
        {
            txtVolumeValue.text = $"{Mathf.RoundToInt(arg0)}";
            SettingManager.Instance.SetSfxVolume(arg0 / (ValueConstants.MAX_SFX * 1f));
        }

        private void OnSliderBgmValueChanged(float arg0)
        {
            txtBgmValue.text = $"{Mathf.RoundToInt(arg0)}";
            SettingManager.Instance.SetBackgroundMusicVolume(arg0 / (ValueConstants.MAX_BGM * 1f));
        }

        private void UnBindEvents()
        {
            sliderFps.onValueChanged.RemoveListener(OnSliderFpsValueChanged);
            sliderBgm.onValueChanged.RemoveListener(OnSliderBgmValueChanged);
            sliderSfx.onValueChanged.RemoveListener(OnSliderSfxValueChanged);
        }

        private void OnOffscreenToggleChanged(bool isOn)
        {
            SettingManager.Instance.SetOffscreenEnabled(isOn);
        }

        private void OnBgMusicToggleChanged(bool isOn)
        {
            SettingManager.Instance.SetBackgroundMusicEnabled(isOn);
        }

        private void OnBattleMusicToggleChanged(bool isOn)
        {
            SettingManager.Instance.SetBattleMusicEnabled(isOn);
        }

        private void OnScreenShakeToggleChanged(bool isOn)
        {
            SettingManager.Instance.SetScreenShakeEnabled(isOn);
        }

        private void OnDamageFontToggleChanged(bool isOn)
        {
            SettingManager.Instance.SetDamageFontEnabled(isOn);
        }

        private void OnContentNotyToggleChanged(bool isOn)
        {
            SettingManager.Instance.SetContentNotiEnabled(isOn);
        }

        private void OnSkillEffectToggleChanged(bool isOn)
        {
            SettingManager.Instance.SetSkillEffectEnabled(isOn);
        }

        private void OnMonsterAppearanceToggleChanged(bool isOn)
        {
            SettingManager.Instance.SetMonsterVisualEnabled(isOn);
        }
    }
}