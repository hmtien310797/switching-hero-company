using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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

        [Header("Auto rotate config")] [SerializeField]
        private RectTransform rtAutoRotate;

        [SerializeField] private float autoRotateDuration;

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
        private Tweener _tweenerAutoRotate;

        private void Awake()
        {
            TransmutationSystemManager.Instance.OnChanged += OnTransmutationSystemChanged;
            TransmutationSystemManager.Instance.OnEquipChanged += OnTransmutationEquipChanged;
            TransmutationSystemManager.Instance.OnSettingChanged += OnTransmutationSettingChanged;

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

        private async void OnEnable()
        {
            // Resync transmutation/list mỗi lần mở màn — tránh hiển thị data cũ leak từ
            // session/account khác (xem Docs/be-transmutation-rpc-spec.md mục 9). Thất bại thì
            // vẫn hiển thị tiếp bằng cache local (SyncFromServerAsync tự log lỗi, không throw).
            await TransmutationSystemManager.Instance.SyncFromServerAsync();

            InitializeEquipment();
            TransmutationSystemManager.Instance.NotifyReady();
        }

        private void OnDestroy()
        {
            TransmutationSystemManager.Instance.OnChanged -= OnTransmutationSystemChanged;
            TransmutationSystemManager.Instance.OnEquipChanged -= OnTransmutationEquipChanged;
            TransmutationSystemManager.Instance.OnSettingChanged -= OnTransmutationSettingChanged;
        }

        private void OnTransmutationEquipChanged(PlayerEquipItem arg1, PlayerEquipItem arg2)
        {
            RebuildEquipment(arg2);
        }

        private void OnTransmutationSettingChanged(TransmutationSystemAutoSettingData arg)
        {
            SetRotate(arg.Enabled);
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
            var setting = TransmutationSystemManager.Instance.Storage.Data.Setting;

            if (setting is not { Enabled: true, })
            {
                UIManager.Instance.OpenPopupAsync<TransmutationSystemAutoSettingView>().Forget();
            }
            else
            {
                TransmutationSystemManager.Instance.SaveSetting(setting.UniqueOptions, setting.Count, setting.Tier, false);
            }
        }

        private async UniTask OnClickTotalStat()
        {
            var ui = await UIManager.Instance.OpenPopupAsync<UITransmutationTotalStatPanel>();
            var modifiers = TransmutationSystemManager.Instance.GetAllModifiers();

            var entries = modifiers
                .Select(pair =>
                {
                    var uniqueCfg = TransmutationSystemManager.Instance.GetUniqueCfg(pair.Key, pair.Value.op);

                    return new TransmutationSystemTotalStatEntry
                    {
                        Value = Convert.ToInt64(pair.Value.pct),
                        IsUnique = pair.Value.isUnique,
                        Title = uniqueCfg?.statName ?? pair.Key.ToString(),
                    };
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
            var newEquip = await TransmutationSystemManager.Instance.FuseIfPossibleAsync();

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
                    await TransmutationSystemManager.Instance.EquipAsync();
                }
            }
        }

        private void InitializeEquipment()
        {
            EmptyAllEquipment();
            var equips = TransmutationSystemManager.Instance.GetEquips();

            foreach (var equip in equips)
            {
                RebuildEquipment(equip);
            }
        }

        private void SetRotate(bool value)
        {
            _tweenerAutoRotate?.Kill();
            _tweenerAutoRotate = null;

            if (value)
            {
                _tweenerAutoRotate = rtAutoRotate
                    .DOLocalRotate(Vector3.forward * 180f, autoRotateDuration, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Incremental);
            }
            else
            {
                rtAutoRotate.rotation = Quaternion.Euler(Vector3.zero);
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
                var cfg = DatabaseManager.Instance.ItemTierDb.Get(equip.ParsedTier);
                equipment.Bind(cfg, equip.Level);
            }
            else
            {
                Debug.LogError($"{equip.ItemType} not found");
            }
        }
    }
}