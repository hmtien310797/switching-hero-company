using System;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shared.Views
{
    /// <summary>
    /// Thông tin dùng để hiển thị popup xác nhận.
    /// </summary>
    public class PopupConfirmArgs
    {
        /// <summary>
        /// Tiêu đề của popup.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Nội dung mô tả hiển thị trong popup.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Nội dung hiển thị trên nút xác nhận.
        /// </summary>
        public string ConfirmButtonText { get; }

        /// <summary>
        /// Nội dung hiển thị trên nút hủy.
        /// </summary>
        public string CancelButtonText { get; }

        /// <summary>
        /// Xác định có hiển thị nút hủy hay không.
        /// Nếu false, popup chỉ hiển thị nút xác nhận.
        /// </summary>
        public bool ShowCancelButton { get; }

        /// <summary>
        /// Callback được gọi khi người dùng nhấn nút xác nhận.
        /// </summary>
        public Action OnConfirm { get; }

        /// <summary>
        /// Callback được gọi khi người dùng nhấn nút hủy hoặc đóng popup.
        /// </summary>
        public Action OnCancel { get; }

        /// <summary>
        /// Khởi tạo thông tin cho popup xác nhận.
        /// </summary>
        /// <param name="title">Tiêu đề của popup.</param>
        /// <param name="description">Nội dung mô tả.</param>
        /// <param name="confirmButtonText">Nội dung nút xác nhận.</param>
        /// <param name="cancelButtonText">Nội dung nút hủy.</param>
        /// <param name="showCancelButton">True để hiển thị nút hủy, false để chỉ hiển thị nút xác nhận.</param>
        /// <param name="onConfirm">Hàm được gọi khi người dùng xác nhận.</param>
        /// <param name="onCancel">Hàm được gọi khi người dùng hủy.</param>
        public PopupConfirmArgs(
            string title,
            string description,
            Action onConfirm = null,
            Action onCancel = null,
            string confirmButtonText = null,
            string cancelButtonText = null,
            bool showCancelButton = true
        )
        {
            Title = title;
            Description = description;
            ConfirmButtonText = confirmButtonText;
            CancelButtonText = cancelButtonText;
            ShowCancelButton = showCancelButton;
            OnConfirm = onConfirm;
            OnCancel = onCancel;
        }
    }

    public class PopupConfirmView : AnimatedUIView
    {
        [SerializeField]
        private TMP_Text txtTitle;

        [SerializeField]
        private TMP_Text txtDesc;

        [SerializeField]
        private Button btnConfirm;

        [SerializeField]
        private Button btnCancel;

        [SerializeField]
        private TMP_Text txtBtnConfirm;

        [SerializeField]
        private TMP_Text txtBtnCancel;

        // --- Private Fields ---
        private Action _onConfirm;
        private Action _onCancel;

        private void Awake()
        {
            btnConfirm.onClick.AddListener(OnClickConfirm);
            btnCancel.onClick.AddListener(OnClickCancel);
        }

        private void OnClickCancel()
        {
            _onCancel?.Invoke();
            UIManager.Instance.Close<PopupConfirmView>();
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);

            if (args is PopupConfirmArgs data)
            {
                Bind(data);
            }
        }

        public void Bind(PopupConfirmArgs args)
        {
            _onConfirm = args.OnConfirm;
            _onCancel = args.OnCancel;

            if (!string.IsNullOrWhiteSpace(args.CancelButtonText))
            {
                txtBtnCancel.text = args.CancelButtonText;
            }

            if (!string.IsNullOrWhiteSpace(args.ConfirmButtonText))
            {
                txtBtnConfirm.text = args.ConfirmButtonText;
            }

            txtDesc.text = args.Description;
            txtTitle.text = args.Title;

            btnCancel.gameObject.SetActive(args.ShowCancelButton);
        }

        private void OnClickConfirm()
        {
            _onConfirm?.Invoke();
            OnClickCancel();
        }
    }
}