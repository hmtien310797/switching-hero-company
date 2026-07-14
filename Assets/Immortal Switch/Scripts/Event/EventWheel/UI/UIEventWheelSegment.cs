using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Shared.UI;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventWheel.UI
{
    public class UIEventWheelSegment : UIItemSlot
    {
        [Header("Reward references")]
        [SerializeField]
        private TextMeshProUGUI txtQuantity;

        [SerializeField]
        private Image imgSegment;

        [PreviewField]
        [SerializeField]
        private Sprite sprSegmentOdd;

        [PreviewField]
        [SerializeField]
        private Sprite sprSegmentEven;

        public Transform Segment => imgSegment.transform;

        public void BindCommon(BigNumber quantity, bool isEven)
        {
            txtQuantity.text = quantity.ToInputString();
            imgSegment.sprite = isEven ? sprSegmentEven : sprSegmentOdd;
        }
    }
}