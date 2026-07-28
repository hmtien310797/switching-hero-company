using System;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shared.Views
{
    /// <summary>
    /// Thông tin dùng để hiển thị popup event info.
    /// </summary>
    public class PopupEventInfoArgs
    {
        /// <summary>
        /// thong tin cua title key can hien thi
        /// </summary>
        public string TitleKey { get; set; }

        /// <summary>
        /// thong tin cua desc key can hien thi
        /// </summary>
        public string DescKey { get; set; }
    }

    public class PopupEventInfoView : AnimatedUIView
    {
        [SerializeField]
        private Button btnClose;

        [SerializeField]
        private TMP_Text txtDesc;

        [SerializeField]
        private TMP_Text txtTitle;

        // --- Private Fields ---
        private PopupEventInfoArgs _args;

        private void Awake()
        {
            btnClose.onClick.AddListener(OnClickClose);
        }

        private void OnDestroy()
        {
            btnClose.onClick.RemoveListener(OnClickClose);
        }

        private void OnClickClose()
        {
            UIManager.Instance.Close<PopupEventInfoView>();
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);

            if (args is not PopupEventInfoArgs runtime)
            {
                _args = null;
                return;
            }

            _args = runtime;

            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (_args == null)
            {
                Debug.LogError("[PopupEventInfoView] Args null");
                return;
            }

            txtTitle.text = LocalizationManager.GetText(_args.TitleKey);
            txtDesc.text = LocalizationManager.GetText(_args.DescKey);
        }
    }
}