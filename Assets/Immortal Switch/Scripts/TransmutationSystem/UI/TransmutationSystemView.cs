using System.Collections.Generic;
using System.Numerics;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.ItemSystem;
using Immortal_Switch.Scripts.ItemSystem.UI;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using Immortal_Switch.Scripts.UI;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.TransmutationSystem.UI
{
    public class TransmutationSystemView : AnimatedUIView
    {
        [Header("Energy")] [Required(InfoMessageType.Error)] [SerializeField]
        private TextMeshProUGUI txtEnergy;

        [Header("Main layout")] [SerializeField]
        private Button btnTransmutation;

        [SerializeField] private ItemSystemView slotWeapon;
        [SerializeField] private ItemSystemView slotGloves;
        [SerializeField] private ItemSystemView slotShield;
        [SerializeField] private ItemSystemView slotHelmet;
        [SerializeField] private ItemSystemView slotArmor;
        [SerializeField] private ItemSystemView slotBoots;
        [SerializeField] private ItemSystemView slotRing;
        [SerializeField] private ItemSystemView slotNecklace;
        [SerializeField] private ItemSystemView slotRelic;
        [SerializeField] private ItemSystemView slotPendant;

        [Header("Replace stuck layout")] [SerializeField]
        private TransmutationSystemReplaceStuckPanel replaceStuckPanel;

        private Dictionary<string, ItemSystemView> _slots = new();

        private void Awake()
        {
            TransmutationSystemManager.Instance.OnEnergyChanged += OnTransmutationSystemEnergyChanged;
            btnTransmutation.onClick.AddListener(OnClickTransmutation);

            _slots = new Dictionary<string, ItemSystemView>
            {
                { ItemSystemTypeConstants.WEAPON, slotWeapon },
                { ItemSystemTypeConstants.GLOVES, slotGloves },
                { ItemSystemTypeConstants.SHIELD, slotShield },
                { ItemSystemTypeConstants.ARMOR, slotArmor },
                { ItemSystemTypeConstants.BOOTS, slotBoots },
                { ItemSystemTypeConstants.RING, slotRing },
                { ItemSystemTypeConstants.NECKLACE, slotNecklace },
                { ItemSystemTypeConstants.RELIC, slotRelic },
                { ItemSystemTypeConstants.PENDANT, slotPendant },
            };
        }

        private void OnDestroy()
        {
            TransmutationSystemManager.Instance.OnEnergyChanged -= OnTransmutationSystemEnergyChanged;
        }

        private void OnTransmutationSystemEnergyChanged(BigInteger obj)
        {
            txtEnergy.SetText(BigIntegerHelper.Format(obj));
        }

        private void Start()
        {
            Initialize();
        }

        private void OnClickTransmutation()
        {
            var newEquip = TransmutationSystemManager.Instance.Transmutation();

            if (newEquip != null)
            {
                var oldEquip = TransmutationSystemManager.Instance.GetEquip(newEquip.ItemType);

                if (oldEquip != null)
                {
                    replaceStuckPanel.gameObject.SetActive(true);
                    replaceStuckPanel.Setup(newEquip, oldEquip);
                }
                else
                {
                    TransmutationSystemManager.Instance.Equip(newEquip, null);
                    replaceStuckPanel.gameObject.SetActive(false);
                }
            }
        }

        private void Initialize()
        {
            var equips = TransmutationSystemManager.Instance.GetEquips();

            foreach (var equip in equips)
            {
                RebuildEquipSlot(equip);
            }
        }

        private void RebuildEquipSlot(PlayerEquipItem equip)
        {
            if (_slots.TryGetValue(equip.ItemType, out var slot))
            {
                // slot.Build(equip);
            }
            else
            {
                Debug.LogError($"{equip.ItemType} not found");
            }
        }
    }
}