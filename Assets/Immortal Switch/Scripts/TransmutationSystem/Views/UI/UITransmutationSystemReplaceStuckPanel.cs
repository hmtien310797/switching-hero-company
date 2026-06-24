using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views.UI
{
    public class UITransmutationSystemReplaceStuckPanel : AnimatedUIView
    {
        [SerializeField] private Button btnEquip;
        [SerializeField] private Button btnDismantle;

        [Header("Equip & Unique Effect")] [SerializeField]
        private UITransmutationSystemReplaceInfoPanel currentReplaceInfo;

        [SerializeField] private UITransmutationSystemReplaceInfoPanel newReplaceInfo;

        // --- Private Field ---
        private PlayerEquipItem _newEquip;
        private PlayerEquipItem _oldEquip;

        private void Awake()
        {
            btnEquip.onClick.AddListener(OnClickEquip);
            btnDismantle.onClick.AddListener(OnClickDismantle);
        }

        public void Setup(PlayerEquipViewData newEquip, PlayerEquipViewData oldEquip)
        {
            _newEquip = newEquip;
            _oldEquip = oldEquip;

            currentReplaceInfo.Bind(oldEquip, null, true);
            newReplaceInfo.Bind(newEquip, oldEquip, false);
        }

        private void OnClickClose()
        {
            UIManager.Instance.TogglePopupAsync<UITransmutationSystemReplaceStuckPanel>().Forget();
        }

        private void OnClickEquip()
        {
            if (_newEquip != null &&
                _oldEquip != null)
            {
                TransmutationSystemManager.Instance.Equip(_newEquip, _oldEquip);
                OnClickClose();

                _newEquip = null;
                _oldEquip = null;
            }
            else
            {
                Debug.LogError("Must have newEquip and oldEquip");
            }
        }

        private void OnClickDismantle()
        {
            TransmutationSystemManager.Instance.Dismantle();
            OnClickClose();

            _newEquip = null;
            _oldEquip = null;
        }
    }
}