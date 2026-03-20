using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    public class SegmentedControlOption : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image selectedBackground;
        [SerializeField] private Image normalBackground;

        private int index;
        private Action<int> onClick;

        public void Initialize(string text, int optionIndex, Action<int> onClickCallback)
        {
            index = optionIndex;
            onClick = onClickCallback;

            if (label != null)
                label.text = text;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClick);
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (selectedBackground != null)
                selectedBackground.enabled = isSelected;

            if (normalBackground != null)
                normalBackground.enabled = !isSelected;
        }

        private void OnClick()
        {
            onClick?.Invoke(index);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }
    }
}