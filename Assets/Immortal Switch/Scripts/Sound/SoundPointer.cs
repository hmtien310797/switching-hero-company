using UnityEngine;
using UnityEngine.EventSystems;

namespace Immortal_Switch.Scripts.Sound
{
    public class SoundPointer : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [Header("Pointer Down")]
        [SerializeField] private bool playOnPointerDown;
        [SerializeField] private SoundId pointerDownSoundId = SoundId.ButtonClick;

        [Header("Pointer Up")]
        [SerializeField] private bool playOnPointerUp;
        [SerializeField] private SoundId pointerUpSoundId = SoundId.ButtonBack;

        [Header("Pointer Enter")]
        [SerializeField] private bool playOnPointerEnter;
        [SerializeField] private SoundId pointerEnterSoundId = SoundId.ButtonClick;

        [Header("Pointer Exit")]
        [SerializeField] private bool playOnPointerExit;
        [SerializeField] private SoundId pointerExitSoundId = SoundId.ButtonBack;

        [Header("Common")]
        [SerializeField] private float volumeMultiplier = 1f;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (playOnPointerDown)
                Play(pointerDownSoundId);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (playOnPointerUp)
                Play(pointerUpSoundId);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (playOnPointerEnter)
                Play(pointerEnterSoundId);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (playOnPointerExit)
                Play(pointerExitSoundId);
        }

        private void Play(SoundId soundId)
        {
            if (SoundManager.Instance == null)
                return;

            SoundManager.Instance.PlaySfx(soundId, volumeMultiplier);
        }
    }
}
