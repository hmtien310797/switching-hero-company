using System.Collections.Generic;
using Immortal_Switch.Scripts.Equipment.UIRuntime;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.TransmutationSystem.UI
{
    public class TransmutationSystemReplaceStuckPanel : MonoBehaviour
    {
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnEquip;
        [SerializeField] private Button btnDismantle;

        [Header("Equip & Unique Effect")] [SerializeField]
        private Transform baseStatLineContainer;

        [SerializeField] private Transform uniqueStatLineContainer;
        [SerializeField] private UIWeaponStatLineItem statLinePrefab;

        // --- Private Field ---
        private List<UIWeaponStatLineItem> _baseStatLineItems = new();
        private List<UIWeaponStatLineItem> _uniqueStatLineItems = new();

        private PlayerEquipItem _newEquip;
        private PlayerEquipItem _oldEquip;

        private void Awake()
        {
            btnClose.onClick.AddListener(OnClickClose);
            btnEquip.onClick.AddListener(OnClickEquip);
            btnDismantle.onClick.AddListener(OnClickDismantle);
        }

        public void Setup(PlayerEquipItem newEquip, PlayerEquipItem oldEquip)
        {
            _newEquip = newEquip;
            _oldEquip = oldEquip;
        }

        private void OnClickClose()
        {
            gameObject.SetActive(false);
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
            if (_newEquip != null &&
                _oldEquip != null)
            {
                TransmutationSystemManager.Instance.Dismantle(_newEquip);
                OnClickClose();

                _newEquip = null;
                _oldEquip = null;
            }
            else
            {
                Debug.LogError("Must have newEquip and oldEquip");
            }
        }

        private void RebuildStatLine(Transform baseStatLineContainer, Transform uniqueStatLineContainer,
            UIWeaponStatLineItem prefab, List<UIWeaponStatLineItem> pools, PlayerEquipItem equip)
        {
            foreach (var modifier in equip.Modifiers)
            {
            }
        }
    }
}