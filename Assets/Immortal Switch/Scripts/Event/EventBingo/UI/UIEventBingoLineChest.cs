using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventBingo.UI
{
    public enum EEventBingoLineId : byte
    {
        // Hàng ngang: line_type = 0
        Row0 = 0,
        Row1 = 1,
        Row2 = 2,
        Row3 = 3,
        Row4 = 4,

        // Hàng dọc: line_type = 1
        Column0 = 5,
        Column1 = 6,
        Column2 = 7,
        Column3 = 8,
        Column4 = 9,

        // Đường chéo: line_type = 2
        DiagonalTopLeftToBottomRight = 10,
        DiagonalTopRightToBottomLeft = 11,
    }

    public class UIEventBingoLineChest : MonoBehaviour
    {
        [SerializeField]
        private EEventBingoLineId lineId;

        [SerializeField]
        private Button btnClaim;

        [SerializeField]
        private GameObject goOverlay;

        [Header("Bg references")]
        [SerializeField]
        private Image imgBg;

        [PreviewField]
        [SerializeField]
        private Sprite sprBgNormal;

        [PreviewField]
        [SerializeField]
        private Sprite sprBgClaimed;

        [Header("Chest references")]
        [SerializeField]
        private Image imgChest;

        [PreviewField]
        [SerializeField]
        private Sprite sprChestNormal;

        [PreviewField]
        [SerializeField]
        private Sprite sprChestClaimed;

        // --- Private Fields ---
        private Action<EEventBingoLineId> _onClickClaim;

        /// <summary>ID line được liên kết với rương này.</summary>
        public EEventBingoLineId LineId => lineId;

        private void Awake()
        {
            btnClaim.onClick.AddListener(OnClickClaim);
        }

        private void OnDestroy()
        {
            btnClaim.onClick.RemoveListener(OnClickClaim);
        }

        private void OnClickClaim()
        {
            _onClickClaim?.Invoke(lineId);
        }

        public void Bind(bool canClaim, bool isClaimed, Action<EEventBingoLineId> onClickClaim)
        {
            _onClickClaim = onClickClaim;
            btnClaim.interactable = canClaim && !isClaimed;

            if (canClaim && !isClaimed)
            {
                imgBg.sprite = sprBgNormal;
                imgChest.sprite = sprChestNormal;

                goOverlay.SetActive(false);
            }
            else if (isClaimed)
            {
                imgBg.sprite = sprBgClaimed;
                imgChest.sprite = sprChestClaimed;

                goOverlay.SetActive(false);
            }
            else
            {
                imgBg.sprite = sprBgNormal;
                imgChest.sprite = sprChestNormal;

                goOverlay.SetActive(true);
            }

            imgChest.SetNativeSize();
        }
    }
}