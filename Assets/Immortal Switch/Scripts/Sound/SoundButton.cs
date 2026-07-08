using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Sound
{
    [RequireComponent(typeof(Button))]
    public class SoundButton : MonoBehaviour
    {
        [SerializeField] private SoundId clickSoundId = SoundId.ButtonClick;
        [SerializeField] private float volumeMultiplier = 1f;
        [SerializeField] private bool randomPitch;
        [SerializeField] private float minPitch = 0.95f;
        [SerializeField] private float maxPitch = 1.05f;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button == null)
                button = GetComponent<Button>();

            button.onClick.AddListener(PlayClickSound);
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(PlayClickSound);
        }

        private void PlayClickSound()
        {
            if (SoundManager.Instance == null)
                return;

            if (randomPitch)
            {
                SoundManager.Instance.PlaySfxRandomPitch(
                    clickSoundId,
                    volumeMultiplier,
                    minPitch,
                    maxPitch);
            }
            else
            {
                SoundManager.Instance.PlaySfx(clickSoundId, volumeMultiplier);
            }
        }
    }
}
