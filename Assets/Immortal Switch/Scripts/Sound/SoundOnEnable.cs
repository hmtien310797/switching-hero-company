using UnityEngine;

namespace Immortal_Switch.Scripts.Sound
{
    public class SoundOnEnable : MonoBehaviour
    {
        [SerializeField] private SoundId soundId = SoundId.PopupOpen;
        [SerializeField] private float volumeMultiplier = 1f;
        [SerializeField] private bool skipFirstEnable = true;

        private bool hasEnabledOnce;

        private void OnEnable()
        {
            if (skipFirstEnable && !hasEnabledOnce)
            {
                hasEnabledOnce = true;
                return;
            }

            if (SoundManager.Instance == null)
                return;

            SoundManager.Instance.PlaySfx(soundId, volumeMultiplier);
        }
    }
}
