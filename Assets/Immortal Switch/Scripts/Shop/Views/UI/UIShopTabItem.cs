using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shop.Views.UI
{
    public class UIShopTabItem : MonoBehaviour
    {
        [SerializeField]
        Button btn;

        [SerializeField]
        TextMeshProUGUI txtTitle;

        /// <summary>
        /// title cua text.
        /// </summary>
        public string Title => txtTitle.text;

        // --- Private Fields ---
        private int _tabIdx;
        private Action<int> _onClick;

        private void Awake()
        {
            if (btn != null)
            {
                btn.onClick.AddListener(OnClick);
            }
        }

        private void OnDestroy()
        {
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnClick);
            }
        }

        private void OnClick()
        {
            _onClick?.Invoke(_tabIdx);
        }

        public void SetSelected(bool selected)
        {
            btn.interactable = !selected;
        }

        public void Bind(int idx, string title, Action<int> onClick)
        {
            _tabIdx = idx;
            _onClick = onClick;

            BindTitle(title);
        }

        public void BindTitle(string title)
        {
            txtTitle.text = title;
        }
    }
}