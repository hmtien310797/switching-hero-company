using System;
using DG.Tweening;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.UI
{
    public class UIEventLeHoiBangLongProgressPanel : MonoBehaviour
    {
        [SerializeField]
        private Button btnClaim;

        [SerializeField]
        private TextMeshProUGUI txtProgress;

        [SerializeField]
        private TextMeshProUGUI txtNote;

        [Header("Vfx references")]
        [SerializeField]
        private RectTransform vfx;

        [SerializeField]
        [Range(0f, 1f)]
        private float rotateDuration;

        [Header("Reward references")]
        [SerializeField]
        private UIItemSlot rewardSlot;

        [SerializeField]
        private Image imgFill;

        // --- Private Fields ---
        private Tweener _vfxTweener;
        private Action _onClickClaim;

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
            _onClickClaim?.Invoke();
        }

        public void Bind(
            Action onClickClaim,
            int summonPoint,
            int targetPoint,
            bool isClaimed,
            int rewardId
        )
        {
            _onClickClaim = onClickClaim;

            var item = DatabaseManager.Instance.ItemDb.FindItem(rewardId);

            txtNote.text = $"Mỗi lần triệu hồi nhận 1 điểm. Đủ {targetPoint} điểm nhận " +
                           $"{LocalizationManager.GetText(item?.itemName)}";

            txtProgress.text = $"{Math.Min(summonPoint, targetPoint)}/{targetPoint}";
            imgFill.fillAmount = targetPoint > 0
                ? Mathf.Clamp01(summonPoint / (float)targetPoint)
                : 0f;

            var canClaim = targetPoint > 0 &&
                           summonPoint >= targetPoint &&
                           !isClaimed;

            btnClaim.interactable = canClaim;

            rewardSlot.Bind(rewardId);
            RefreshVfx(canClaim);
        }

        public void RefreshVfx(bool active)
        {
            if (active)
            {
                _vfxTweener?.Kill();
                vfx.gameObject.SetActive(true);

                _vfxTweener = vfx.transform
                    .DOLocalRotate(
                        Vector3.forward * 360f,
                        rotateDuration,
                        RotateMode.FastBeyond360
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
