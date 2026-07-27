using System;
using DG.Tweening;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Shop.Views.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventLogin.UI
{
    public class UIEventLoginMilestoneTarget : MonoBehaviour
    {
        [SerializeField]
        private Button btnClaim;

        [SerializeField]
        private UIShopProductItem productItem;

        [SerializeField]
        private TextMeshProUGUI txtPoint;

        [SerializeField]
        private GameObject overlayClaimed;

        [Header("Vfx references")]
        [SerializeField]
        private RectTransform vfx;

        [SerializeField]
        [Range(0f, float.MaxValue)]
        private float rotateDuration = 0.25f;

        // --- Private Fields ---
        private Action<int> _onClickClaim;
        private Tweener _vfxTweener;

        private int _milestoneId;

        private void Awake()
        {
            btnClaim.onClick.AddListener(OnClickClaim);
        }

        private void OnDestroy()
        {
            Kill();
            btnClaim.onClick.RemoveListener(OnClickClaim);
        }

        private void OnClickClaim()
        {
            _onClickClaim?.Invoke(_milestoneId);
        }

        public void Bind(int currentPoint, int itemId, BigNumber quantity, int targetPoint, bool isClaimed,
            Action<int> onClickClaim, int milestoneId)
        {
            _milestoneId = milestoneId;
            _onClickClaim = onClickClaim;

            txtPoint.text = $"{targetPoint}";

            var canClaim = currentPoint >= targetPoint &&
                           !isClaimed;

            btnClaim.interactable = canClaim;

            RefreshVfx(canClaim);
            overlayClaimed.SetActive(isClaimed);
            productItem.Bind(itemId, quantity);
        }

        private void Kill()
        {
            _vfxTweener?.Kill();
            _vfxTweener = null;
        }

        public void RefreshVfx(bool active)
        {
            if (active)
            {
                Kill();
                vfx.gameObject.SetActive(true);

                _vfxTweener = vfx.transform
                    .DOLocalRotate(
                        Vector3.forward * 360f,
                        rotateDuration,
                        RotateMode.LocalAxisAdd
                    )
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Incremental);
            }
            else
            {
                vfx.gameObject.SetActive(false);
            }
        }
    }
}