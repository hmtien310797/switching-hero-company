using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Sound
{
    [RequireComponent(typeof(Slider))]
    public class SoundSlider : MonoBehaviour
    {
        [SerializeField] private SoundId valueChangedSoundId = SoundId.SliderTick;
        [SerializeField] private float volumeMultiplier = 0.5f;
        [SerializeField] private float cooldown = 0.08f;
        [SerializeField] private bool skipFirstCallback = true;

        private Slider slider;
        private bool hasInitialized;
        private float lastPlayTime;

        private void Awake()
        {
            slider = GetComponent<Slider>();
        }

        private void OnEnable()
        {
            if (slider == null)
                slider = GetComponent<Slider>();

            slider.onValueChanged.AddListener(HandleValueChanged);
        }

        private void OnDisable()
        {
            if (slider != null)
                slider.onValueChanged.RemoveListener(HandleValueChanged);
        }

        private void HandleValueChanged(float value)
        {
            if (skipFirstCallback && !hasInitialized)
            {
                hasInitialized = true;
                return;
            }

            if (SoundManager.Instance == null)
                return;

            if (Time.unscaledTime - lastPlayTime < cooldown)
                return;

            lastPlayTime = Time.unscaledTime;
            SoundManager.Instance.PlaySfx(valueChangedSoundId, volumeMultiplier);
        }
    }
}
