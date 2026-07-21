using System;
using Cysharp.Threading.Tasks;
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
        /// Callback được gọi khi người dùng nhấn bật toggle Không hiển thị lại thông báo.
        /// </summary>
        public Action<bool> OnDoNotShowAgainChanged { get; }

        /// <summary>
        /// Xác định có hiển thị toggle Không hiển thị lại thông báo hay không.
        /// </summary>
        public bool ShowToggleDoNotShowAgain { get; }

        /// <summary>
        /// Khởi tạo thông tin cho popup xác nhận.
        /// </summary>
        /// <param name="title">Tiêu đề của popup.</param>
        /// <param name="description">Nội dung mô tả.</param>
        /// <param name="confirmButtonText">Nội dung nút xác nhận.</param>
        /// <param name="cancelButtonText">Nội dung nút hủy.</param>
        /// <param name="showCancelButton">True để hiển thị nút hủy, false để chỉ hiển thị nút xác nhận.</param>
        /// <param name="showToggleDoNotShowAgain">True để hiển thị toggle Không hiển thị lại thông báo hay không.</param>
        /// <param name="onConfirm">Hàm được gọi khi người dùng xác nhận.</param>
        /// <param name="onCancel">Hàm được gọi khi người dùng hủy.</param>
        /// <param name="onDoNotShowAgainChanged">Hàm được gọi khi người dùng nhấn bật toggle Không hiển thị lại thông báo.</param>
        public PopupConfirmArgs(
            string title,
            string description,
            Action onConfirm = null,
            Action onCancel = null,
            Action<bool> onDoNotShowAgainChanged = null,
            string confirmButtonText = null,
            string cancelButtonText = null,
            bool showCancelButton = true,
            bool showToggleDoNotShowAgain = true
        )
        {
            Title = title;
            Description = description;
            ConfirmButtonText = confirmButtonText;
            CancelButtonText = cancelButtonText;
            ShowCancelButton = showCancelButton;
            OnConfirm = onConfirm;
            OnCancel = onCancel;
            OnDoNotShowAgainChanged = onDoNotShowAgainChanged;
            ShowToggleDoNotShowAgain = showToggleDoNotShowAgain;
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

        [SerializeField]
        private Toggle toggleDoNotShowAgain;

        // --- Private Fields ---
        private PopupConfirmArgs _args;

        private void Awake()
        {
            btnConfirm.onClick.AddListener(OnClickConfirm);
            btnCancel.onClick.AddListener(OnClickCancel);
            toggleDoNotShowAgain.onValueChanged.AddListener(OnDoNotShowAgainChanged);
        }

        private void OnDestroy()
        {
            btnConfirm.onClick.RemoveListener(OnClickConfirm);
            btnCancel.onClick.RemoveListener(OnClickCancel);
            toggleDoNotShowAgain.onValueChanged.RemoveListener(OnDoNotShowAgainChanged);
        }

        private void OnDoNotShowAgainChanged(bool arg0)
        {
            _args.OnDoNotShowAgainChanged?.Invoke(arg0);
        }

        private void OnClickCancel()
        {
            _args.OnCancel?.Invoke();
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
            _args = args;

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

            toggleDoNotShowAgain.gameObject.SetActive(args.ShowToggleDoNotShowAgain);
            btnCancel.gameObject.SetActive(args.ShowCancelButton);
        }

        private void OnClickConfirm()
        {
            _args.OnConfirm?.Invoke();
            OnClickCancel();
        }
    }
    
    public static class PopupConfirmService
    {
        public static void Show(
            string title,
            string description,
            Action onConfirm = null,
            Action onCancel = null,
            Action<bool> onDoNotShowAgainChanged = null,
            string confirmButtonText = null,
            string cancelButtonText = null,
            bool showCancelButton = true,
            bool showToggleDoNotShowAgain = false,
            bool withBackdrop = false)
        {
            if (UIManager.Instance == null)
            {
                Debug.LogError("[PopupConfirmService] UIManager.Instance chưa được khởi tạo.");
                return;
            }

            var args = new PopupConfirmArgs(
                title: title,
                description: description,
                onConfirm: onConfirm,
                onCancel: onCancel,
                onDoNotShowAgainChanged: onDoNotShowAgainChanged,
                confirmButtonText: confirmButtonText,
                cancelButtonText: cancelButtonText,
                showCancelButton: showCancelButton,
                showToggleDoNotShowAgain: showToggleDoNotShowAgain);

            UIManager.Instance
                .OpenPopupAsync<PopupConfirmView>(args, withBackdrop)
                .Forget();
        }

        public static void ShowNotice(
            string title,
            string description,
            Action onConfirm = null,
            string confirmButtonText = null,
            bool withBackdrop = false)
        {
            Show(
                title: title,
                description: description,
                onConfirm: onConfirm,
                confirmButtonText: confirmButtonText,
                showCancelButton: false,
                showToggleDoNotShowAgain: false,
                withBackdrop: withBackdrop);
        }

        public static void Close()
        {
            if (UIManager.Instance == null)
                return;

            UIManager.Instance.Close<PopupConfirmView>();
        }
    }
}