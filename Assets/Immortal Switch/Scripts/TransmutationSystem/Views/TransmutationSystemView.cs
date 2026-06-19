using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.ItemSystem;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.TransmutationSystem.Models;
using Immortal_Switch.Scripts.TransmutationSystem.Views.UI;
using Immortal_Switch.Scripts.UI;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views
{
    public class TransmutationSystemView : AnimatedUIView
    {
        [Header("Progress")] [SerializeField] private Image imgProgress;
        [SerializeField] private TextMeshProUGUI txtProgress;
        [SerializeField] private TextMeshProUGUI txtLevel;

        [Header("Energy")] [Required(InfoMessageType.Error)] [SerializeField]
        private TextMeshProUGUI txtEnergy;

        [Header("Main layout")] [SerializeField]
        private Button btnTransmutation;

        [SerializeField] private Button btnTotalStat;
        [SerializeField] private Button btnAuto;
        [SerializeField] private Button btnHelp;

        [Header("Slot layout")] [SerializeField]
        private UITransmutationEquipment slotWeapon;

        [SerializeField] private UITransmutationEquipment slotGloves;
        [SerializeField] private UITransmutationEquipment slotShield;
        [SerializeField] private UITransmutationEquipment slotHelmet;
        [SerializeField] private UITransmutationEquipment slotArmor;
        [SerializeField] private UITransmutationEquipment slotBoots;
        [SerializeField] private UITransmutationEquipment slotRing;
        [SerializeField] private UITransmutationEquipment slotNecklace;
        [SerializeField] private UITransmutationEquipment slotRelic;
        [SerializeField] private UITransmutationEquipment slotPendant;

        // --- Private Fields ---
        private Dictionary<string, UITransmutationEquipment> _equipments = new();

        private void Awake()
        {
            TransmutationSystemManager.Instance.OnChanged += OnTransmutationSystemChanged;
            btnTransmutation.onClick.AddListener(() => OnClickTransmutation().Forget());
            btnAuto.onClick.AddListener(OnClickAuto);
            btnTotalStat.onClick.AddListener(() => OnClickTotalStat().Forget());
            btnHelp.onClick.AddListener(() => OnClickHelp().Forget());

            _equipments = new Dictionary<string, UITransmutationEquipment>
            {
                { ItemSystemTypeConstants.WEAPON, slotWeapon },
                { ItemSystemTypeConstants.GLOVES, slotGloves },
                { ItemSystemTypeConstants.SHIELD, slotShield },
                { ItemSystemTypeConstants.HELMET, slotHelmet },
                { ItemSystemTypeConstants.ARMOR, slotArmor },
                { ItemSystemTypeConstants.BOOTS, slotBoots },
                { ItemSystemTypeConstants.RING, slotRing },
                { ItemSystemTypeConstants.NECKLACE, slotNecklace },
                { ItemSystemTypeConstants.RELIC, slotRelic },
                { ItemSystemTypeConstants.PENDANT, slotPendant },
            };
        }

        private void OnEnable()
        {
            Initialize();
            TransmutationSystemManager.Instance.NotifyReady();
        }

        private void OnDestroy()
        {
            TransmutationSystemManager.Instance.OnChanged -= OnTransmutationSystemChanged;
        }

        private void OnTransmutationSystemChanged(TransmutationSystemChanged arg)
        {
            imgProgress.fillAmount = arg.Progress;
            txtProgress.SetText($"{BigIntegerHelper.Format(arg.Data.Exp)} / {BigIntegerHelper.Format(arg.TargetExp)}");
            txtLevel.SetText($"Cấp Độ Dung Hợp {arg.Data.Level:00}");
            txtEnergy.SetText(BigIntegerHelper.Format(arg.Data.Energy));
        }

        private async UniTask OnClickHelp()
        {
            var ui = await UIManager.Instance.OpenPopupAsync<UITransmutationSystemLevelInfoPanel>();
            var manager = TransmutationSystemManager.Instance;
            ui.Bind(manager.Database.RateConfig.rows, manager.Storage.Data.Level);
        }

        private void OnClickAuto()
        {
            UIManager.Instance.OpenPopupAsync<TransmutationSystemAutoSettingView>().Forget();
        }

        private async UniTask OnClickTotalStat()
        {
            var rnd = new Random();
            var ui = await UIManager.Instance.OpenPopupAsync<UITransmutationTotalStatPanel>();
            var modifiers = TransmutationSystemManager.Instance.GetAllModifiers();

            var entries = modifiers
                .Select(pair => new TransmutationSystemTotalStatEntry
                {
                    Value = Convert.ToInt64(pair.Value),
                    IsUnique = rnd.Next(0, 10) > 5,
                    Title = pair.Key.ToString(),
                })
                .OrderBy(v => v.IsUnique)
                .ToList();

            ui.Bind(new TransmutationSystemTotalStatData
            {
                Entries = entries,
            });
        }

        private async UniTask OnClickTransmutation()
        {
            var newEquip = TransmutationSystemManager.Instance.Fuse();

            if (newEquip != null)
            {
                var oldEquip = TransmutationSystemManager.Instance.GetEquip(newEquip.ItemType);

                if (oldEquip != null)
                {
                    var ui = await UIManager.Instance.OpenPopupAsync<UITransmutationSystemReplaceStuckPanel>();
                    ui.Setup(newEquip, oldEquip);
                }
                else
                {
                    TransmutationSystemManager.Instance.Equip(newEquip, null);
                    UIManager.Instance.TogglePopupAsync<UITransmutationSystemReplaceStuckPanel>().Forget();
                }
            }
        }

        private void Initialize()
        {
            EmptyAllEquipment();
            var equips = TransmutationSystemManager.Instance.GetEquips();

            foreach (var equip in equips)
            {
                RebuildEquipment(equip);
            }
        }

        private void EmptyAllEquipment()
        {
            foreach (var pair in _equipments)
            {
                pair.Value.SetEmpty(true);
            }
        }

        private void RebuildEquipment(PlayerEquipItem equip)
        {
            if (_equipments.TryGetValue(equip.ItemType, out var equipment))
            {
                var cfg = DatabaseManager.Instance.EquipmentTierDatabase.Get(equip.ParsedTier);
                equipment.Bind(cfg, equip.Level);
            }
            else
            {
                Debug.LogError($"{equip.ItemType} not found");
            }
        }
    }
}