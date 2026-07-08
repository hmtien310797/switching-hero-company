using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Sound
{
    [RequireComponent(typeof(Toggle))]
    public class SoundToggle : MonoBehaviour
    {
        [SerializeField] private SoundId onSoundId = SoundId.ToggleOn;
        [SerializeField] private SoundId offSoundId = SoundId.ToggleOff;
        [SerializeField] private float volumeMultiplier = 1f;
        [SerializeField] private bool skipFirstCallback = true;

        private Toggle toggle;
        private bool hasInitialized;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
        }

        private void OnEnable()
        {
            if (toggle == null)
                toggle = GetComponent<Toggle>();

            toggle.onValueChanged.AddListener(HandleValueChanged);
        }

        private void OnDisable()
        {
            if (toggle != null)
                toggle.onValueChanged.RemoveListener(HandleValueChanged);
        }

        private void HandleValueChanged(bool isOn)
        {
            if (skipFirstCallback && !hasInitialized)
            {
                hasInitialized = true;
                return;
            }

            if (SoundManager.Instance == null)
                return;

            SoundManager.Instance.PlaySfx(isOn ? onSoundId : offSoundId, volumeMultiplier);
        }
    }
}
