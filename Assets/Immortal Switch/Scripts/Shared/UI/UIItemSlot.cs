using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shared.UI
{
    public class UIItemSlot : MonoBehaviour
    {
        [Header("View references")]
        [SerializeField]
        private Image imgItemIcon;

        [SerializeField]
        private Image imgBorder;

        [SerializeField]
        private Image imgBg;

        [SerializeField]
        private Image imgTier;

        [SerializeField]
        private Button btn;

        // --- Private Fields ---
        private bool _allowShowInfo;
        private int _itemId;

        private void Awake()
        {
            if (btn != null)
            {
                btn.onClick.AddListener(OnClickItemInfo);
            }
        }

        private void OnDestroy()
        {
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnClickItemInfo);
            }
        }

        private void OnClickItemInfo()
        {
            if (_itemId != 0 && _allowShowInfo)
            {
                UIManager.Instance
                    .OpenPopupAsync<PopupItemInfoView>(new PopupItemInfoArgs
                    {
                        ItemId = _itemId,
                    })
                    .Forget();
            }
        }

        public void Bind(int itemId, bool allowShowInfo = false)
        {
            _itemId = itemId;
            _allowShowInfo = allowShowInfo;

            var itemDisplay = DatabaseManager.Instance.GetDisplayData(itemId);

            if (itemDisplay != null)
            {
                Bind(itemDisplay.ItemIcon,
                    itemDisplay.TierInfo.border,
                    itemDisplay.TierInfo.background,
                    itemDisplay.TierInfo.tierIcon,
                    allowShowInfo
                );
            }
        }

        public void Bind(Sprite itemIcon, Sprite borderIcon, Sprite bgIcon, Sprite tierIcon, bool allowShowInfo = false)
        {
            _allowShowInfo = allowShowInfo;
            imgItemIcon.sprite = itemIcon;
            imgBorder.sprite = borderIcon;
            imgBg.sprite = bgIcon;
            imgTier.sprite = tierIcon;
        }
    }
}