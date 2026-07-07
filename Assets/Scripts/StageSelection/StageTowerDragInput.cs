using UnityEngine;
using UnityEngine.EventSystems;

namespace Immortal_Switch.Scripts.StageSelection
{
    public class StageTowerDragInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private StageSelectionController controller;
        [SerializeField] private float dragStepThreshold = 80f;

        private float accumulatedY;

        public void OnBeginDrag(PointerEventData eventData)
        {
            accumulatedY = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {

        }

        public void OnEndDrag(PointerEventData eventData)
        {
            accumulatedY = 0f;
        }
    }
}