using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Profile.Models;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views.UI
{
    public class UITransmutationReplaceStuckPanel : UIView
    {
        [SerializeField]
        private Button btnEquip;

        [SerializeField]
        private Button btnDismantle;

        [Header("Equip & Unique Effect")]
        [SerializeField]
        private UITransmutationSystemReplaceInfoPanel currentReplaceInfo;

        [SerializeField]
        private UITransmutationSystemReplaceInfoPanel newReplaceInfo;

        [SerializeField]
        private float replaceInfoSwitchDuration = 0.25f;

        // --- Private Field ---
        private PlayerEquipViewData _newEquip;
        private PlayerEquipViewData _oldEquip;

        private Sequence _sequenceSwitchEquip;
        private RectTransform _rtCurrentReplaceInfo;
        private RectTransform _rtNewReplaceInfo;

        private float _orgPosYCurrentReplaceInfo;
        private float _orgPosYNewReplaceInfo;

        private Vector2 _orgSizeCurrentReplaceInfo;
        private Vector2 _orgSizeNewReplaceInfo;
        private bool _isNewInfoMovingUp;

        private void Awake()
        {
            _rtCurrentReplaceInfo = currentReplaceInfo.transform as RectTransform;
            _rtNewReplaceInfo = newReplaceInfo.transform as RectTransform;

            if (_rtCurrentReplaceInfo != null &&
                _rtNewReplaceInfo != null)
            {
                _orgPosYCurrentReplaceInfo = _rtCurrentReplaceInfo.anchoredPosition.y;
                _orgPosYNewReplaceInfo = _rtNewReplaceInfo.anchoredPosition.y;

                _orgSizeCurrentReplaceInfo = _rtCurrentReplaceInfo.sizeDelta;
                _orgSizeNewReplaceInfo = _rtNewReplaceInfo.sizeDelta;
            }

            btnEquip.onClick.AddListener(OnClickEquip);
            btnDismantle.onClick.AddListener(OnClickDismantle);
        }

        public void Setup(PlayerEquipViewData newEquip, PlayerEquipViewData oldEquip)
        {
            _newEquip = newEquip;
            _oldEquip = oldEquip;

            if (oldEquip != null)
            {
                btnDismantle.gameObject.SetActive(true);
                currentReplaceInfo.gameObject.SetActive(true);
                currentReplaceInfo.Bind(oldEquip, null, true);
            }
            else
            {
                currentReplaceInfo.gameObject.SetActive(false);
                btnDismantle.gameObject.SetActive(false);
            }

            newReplaceInfo.Bind(newEquip, oldEquip, false);
        }

        private void OnClickClose()
        {
            UIManager.Instance.TogglePopupAsync<UITransmutationReplaceStuckPanel>().Forget();
        }

        private async void OnClickEquip()
        {
            // slot chua co trang bị nào.
            if (_newEquip != null &&
                _oldEquip == null)
            {
                await TransmutationSystemManager.Instance.EquipAsync();
                OnClickClose();

                _newEquip = null;
                _oldEquip = null;
            }
            else if (_newEquip != null &&
                     _oldEquip != null)
            {
                SwitchEquip();
            }
            else
            {
                Debug.LogError("Must have newEquip or oldEquip");
            }
        }

        private async void OnClickDismantle()
        {
            var success = await TransmutationSystemManager.Instance.DismantleAsync();

            if (!success)
            {
                return;
            }

            OnClickClose();

            _newEquip = null;
            _oldEquip = null;
        }

        private void SwitchEquip()
        {
            _sequenceSwitchEquip?.Kill();

            _sequenceSwitchEquip = DOTween.Sequence()
                .OnStart(SwitchEquipStart)
                .Join(_rtCurrentReplaceInfo.DOAnchorPosY(_orgPosYNewReplaceInfo, replaceInfoSwitchDuration))
                .Join(_rtCurrentReplaceInfo.DOSizeDelta(_orgSizeNewReplaceInfo, replaceInfoSwitchDuration))
                .Join(_rtNewReplaceInfo.DOAnchorPosY(_orgPosYCurrentReplaceInfo, replaceInfoSwitchDuration))
                .Join(_rtNewReplaceInfo.DOSizeDelta(_orgSizeCurrentReplaceInfo, replaceInfoSwitchDuration))
                .OnComplete(() => SwitchEquipCompleteAsync().Forget());
        }

        private void SwitchEquipStart()
        {
            RefreshButton(false);
            currentReplaceInfo.HideUsedLayout();
            newReplaceInfo.HideUsedLayout();
        }

        private async UniTaskVoid SwitchEquipCompleteAsync()
        {
            (_orgSizeCurrentReplaceInfo, _orgSizeNewReplaceInfo) = (_orgSizeNewReplaceInfo, _orgSizeCurrentReplaceInfo);
            (_orgPosYCurrentReplaceInfo, _orgPosYNewReplaceInfo) = (_orgPosYNewReplaceInfo, _orgPosYCurrentReplaceInfo);
            (_oldEquip, _newEquip) = (_newEquip, _oldEquip);
            _isNewInfoMovingUp = !_isNewInfoMovingUp;

            if (_isNewInfoMovingUp)
            {
                currentReplaceInfo.Bind(_newEquip, _oldEquip, false);
                newReplaceInfo.Bind(_oldEquip, _newEquip, true);
            }
            else
            {
                currentReplaceInfo.Bind(_oldEquip, _newEquip, true);
                newReplaceInfo.Bind(_newEquip, _oldEquip, false);
            }

            // Sau khi anim doi vi tri xong, chot equip that su voi server (server chi biet
            // 1 pending item duy nhat, luon la item vua fuse ra bat ke da doi vi tri hien thi hay chua).
            var success = await TransmutationSystemManager.Instance.EquipAsync();

            if (!success)
            {
                RefreshButton(true);
                return;
            }

            OnClickClose();

            _newEquip = null;
            _oldEquip = null;
        }

        private void RefreshButton(bool value)
        {
            btnDismantle.interactable = value;
            btnEquip.interactable = value;
        }
    }
}