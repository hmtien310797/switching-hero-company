using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    public class SwipeToUnlockControl : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [SerializeField] private RectTransform track;
        [SerializeField] private Slider slider;
        [SerializeField] private float unlockThreshold = 0.85f;
        [SerializeField] private float returnSpeed = 3f;

        private bool dragging;
        private bool unlocked;

        public event Action OnUnlocked;

        private void Update()
        {
            if (dragging || unlocked || slider == null)
                return;

            if (slider.value > 0f)
                slider.value = Mathf.MoveTowards(
                    slider.value,
                    0f,
                    returnSpeed * Time.deltaTime
                );
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (unlocked)
                return;

            dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (unlocked || slider == null || track == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                track,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint
            );

            float width = track.rect.width;

            float value = Mathf.InverseLerp(
                -width * 0.5f,
                width * 0.5f,
                localPoint.x
            );

            slider.value = Mathf.Clamp01(value);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (unlocked || slider == null)
                return;

            dragging = false;

            if (slider.value >= unlockThreshold)
            {
                unlocked = true;
                slider.value = 1f;

                OnUnlocked?.Invoke();
            }
        }

        public void ResetState()
        {
            dragging = false;
            unlocked = false;

            if (slider != null)
                slider.value = 0f;
        }
    }
}