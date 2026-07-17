using Immortal_Switch.Scripts.Items;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shared.Views
{
    /// <summary>
    /// Thông tin dùng để hiển thị popup item info.
    /// </summary>
    public class PopupItemInfoArgs
    {
        /// <summary>
        /// thong tin cua item can hien thi
        /// </summary>
        public int ItemId { get; set; }
    }

    public class PopupItemInfoView : AnimatedUIView
    {
        [SerializeField]
        private TMP_Text txtDesc;

        [SerializeField]
        private TMP_Text txtQuantity;

        [SerializeField]
        private TMP_Text txtTitle;

        [SerializeField]
        private Image imgIcon;

        // --- Private Fields ---
        private PopupItemInfoArgs _args;

        public override void OnShow(object args)
        {
            base.OnShow(args);

            if (args is not PopupItemInfoArgs runtime)
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
                Debug.LogError("[PopupItemInfoView] Args null");
                return;
            }

            var item = DatabaseManager.Instance.ItemDb.FindItem(_args.ItemId);

            if (item != null)
            {
                var itemIcon = DatabaseManager.Instance.ItemDb.LoadIcon(item.rarity, item.itemType, item.itemKey);
                var itemQuantity = ItemsManager.Instance.GetQuantity(_args.ItemId);

                imgIcon.sprite = itemIcon;
                txtTitle.text = LocalizationManager.GetText(item.itemName);
                txtDesc.text = LocalizationManager.GetText(item.desc);
                txtQuantity.text = $"Số lượng: {itemQuantity.ToInputString()}";
            }
        }
    }
}