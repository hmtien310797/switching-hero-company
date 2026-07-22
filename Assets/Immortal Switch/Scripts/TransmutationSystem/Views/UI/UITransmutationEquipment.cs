using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Items.ScriptableObjects;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views.UI
{
    public class UITransmutationEquipment : MonoBehaviour
    {
        [Header("Item view")]
        [SerializeField]
        private Image imgTier;

        [SerializeField]
        private Image imgIcon;

        [SerializeField]
        private Image imgBorder;

        [SerializeField]
        private Image imgBg;

        [SerializeField]
        private TMP_Text txtLevel;

        [SerializeField]
        private GameObject goEmpty;

        [SerializeField]
        private Button btn;

        // --- Private Fields ---
        private PlayerEquipViewData _vm;

        private void Awake()
        {
            if (btn != null)
            {
                btn.onClick.AddListener(OnClickEquipmentInfo);
            }
        }

        private void OnDestroy()
        {
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnClickEquipmentInfo);
            }
        }

        private void OnClickEquipmentInfo()
        {
            if (_vm != null)
            {
                UIManager.Instance
                    .OpenPopupAsync<UITransmutationEquipmentInfoPanel>(new UITransmutationEquipmentInfoArgs
                    {
                        EquipView = _vm,
                    })
                    .Forget();
            }
        }

        public void SetEmpty(bool value)
        {
            if (goEmpty != null)
            {
                goEmpty.SetActive(value);
            }
        }

        public void Bind(PlayerEquipViewData vm, int level)
        {
            _vm = vm;

            var cfg = ItemTierVisualImageService.GetItemTierEntry(vm.ParsedTier);

            SetEmpty(false);

            if (cfg != null)
            {
                txtLevel.text = $"Lv {level:00}";
                imgBg.sprite = cfg.background;
                imgBorder.sprite = cfg.border;
                imgTier.sprite = cfg.tierIcon;
            }
        }
    }
}