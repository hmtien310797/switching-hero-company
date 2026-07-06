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
        private Action<int> onClickWithIndex;
        private Action onClickSimple;

        /// <summary>
        /// Kiểu cũ: dùng cho SegmentedControl spawn option runtime.
        /// </summary>
        public void Initialize(string text, int optionIndex, Action<int> onClickCallback)
        {
            index = optionIndex;
            onClickWithIndex = onClickCallback;
            onClickSimple = null;

            if (label != null)
                label.text = text;

            BindButton();
        }

        /// <summary>
        /// Kiểu mới: dùng cho SegmentedControlStatic với option đã đặt sẵn trong scene.
        /// </summary>
        public void Bind(Action callback)
        {
            onClickSimple = callback;
            onClickWithIndex = null;

            BindButton();
        }

        /// <summary>
        /// Optional: nếu muốn đổi text của option prebuilt.
        /// </summary>
        public void SetLabel(string text)
        {
            if (label != null)
                label.text = text;
        }

        public void SetSelected(bool isSelected)
        {
            if (selectedBackground != null)
                selectedBackground.gameObject.SetActive(isSelected);

            if (normalBackground != null)
                normalBackground.gameObject.SetActive(!isSelected);
        }

        private void BindButton()
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (onClickSimple != null)
            {
                onClickSimple.Invoke();
                return;
            }

            onClickWithIndex?.Invoke(index);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }
    }
}