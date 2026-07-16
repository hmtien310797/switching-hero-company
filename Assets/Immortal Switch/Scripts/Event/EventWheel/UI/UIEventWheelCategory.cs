using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventWheel.UI
{
    public class UIEventWheelCategory : MonoBehaviour
    {
        [SerializeField]
        private Image img;

        [PreviewField]
        [SerializeField]
        private Sprite sprSelect;

        [PreviewField]
        [SerializeField]
        private Sprite sprNormal;

        public void SetSelected(bool selected)
        {
            img.sprite = selected ? sprSelect : sprNormal;
        }
    }
}