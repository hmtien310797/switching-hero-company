using UnityEngine;

namespace Immortal_Switch.Scripts.Sound
{
    public class SoundOnDisable : MonoBehaviour
    {
        [SerializeField] private SoundId soundId = SoundId.PopupClose;
        [SerializeField] private float volumeMultiplier = 1f;
        [SerializeField] private bool skipIfApplicationQuitting = true;

        private static bool isApplicationQuitting;

        private void OnApplicationQuit()
        {
            isApplicationQuitting = true;
        }

        private void OnDisable()
        {
            if (skipIfApplicationQuitting && isApplicationQuitting)
                return;

            if (SoundManager.Instance == null)
                return;

            SoundManager.Instance.PlaySfx(soundId, volumeMultiplier);
        }
    }
}
