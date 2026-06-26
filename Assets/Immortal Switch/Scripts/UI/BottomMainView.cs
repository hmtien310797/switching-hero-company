using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.DungeonSystem.Views;
using Immortal_Switch.Scripts.GrowthSystem.UI;
using Immortal_Switch.Scripts.HeroUIView;
using Immortal_Switch.Scripts.MissionSystem.Views;
using Immortal_Switch.Scripts.SummonSystem.Shared.UI;
using Immortal_Switch.Scripts.TransmutationSystem;
using Immortal_Switch.Scripts.TransmutationSystem.Models;
using Immortal_Switch.Scripts.TransmutationSystem.Views;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    public class BottomMainView : UIView
    {
        [SerializeField] private BottomMainButton ButtonShop;
        [SerializeField] private BottomMainButton ButtonHero;
        [SerializeField] private BottomMainButton ButtonGrowth;
        [SerializeField] private BottomMainButton ButtonEquip;
        [SerializeField] private BottomMainButton ButtonMission;
        [SerializeField] private BottomMainButton ButtonDungeon;
        [SerializeField] private Button ButtonGem;
        [SerializeField] private Button ButtonClose;
        [SerializeField] private GameObject Gem;

        [Header("Energy")] [Required(InfoMessageType.Error)] [SerializeField]
        private TextMeshProUGUI txtEnergy;

        // --- Private Field ---
        private BottomMainButton _selectedBtn;

        private void Awake()
        {
            // Ensure persistent layer (recommended)
            Layer = UILayer.SubMain;

            if (ButtonGrowth != null)
                ButtonGrowth.AddListener(() => OnToggleMain<GrowthView>(ButtonGrowth).Forget());

            if (ButtonEquip != null)
                ButtonEquip.AddListener(() => OnToggleMain<EquipView>(ButtonEquip).Forget());

            ButtonShop.AddListener(() => OnToggleMain<SummonHubView>(ButtonShop).Forget());
            ButtonHero.AddListener(() => OnToggleMain<HeroCollectionView>(ButtonHero).Forget());
            ButtonMission.AddListener(() => OnToggleMain<MissionSystemView>(ButtonMission).Forget());
            ButtonGem.onClick.AddListener(() => OnToggleMain<TransmutationSystemView>(null, false).Forget());
            ButtonDungeon.AddListener(() => OnToggleMain<DungeonMainView>(ButtonDungeon).Forget());

            if (ButtonClose != null)
                ButtonClose.onClick.AddListener(OnClickClose);
        }

        private void Start()
        {
            TransmutationSystemManager.Instance.OnChanged += OnTransmutationSystemChanged;
            GameEventManager.Subscribe(GameEvents.OnToggleMainView, RefreshCloseAndGem);
        }
        
        private void OnDestroy()
        {
            TransmutationSystemManager.Instance.OnChanged -= OnTransmutationSystemChanged;
            GameEventManager.Unsubscribe(GameEvents.OnToggleMainView, RefreshCloseAndGem);
        }

        private void OnTransmutationSystemChanged(TransmutationSystemChanged obj)
        {
            txtEnergy.SetText(BigIntegerHelper.Format(obj.Data.Energy));
        }
        

        private void OnEnable()
        {
            RefreshCloseAndGem();
        }

        private async UniTaskVoid OnToggleMain<T>([CanBeNull] BottomMainButton selected, bool withBackdrop = true)
            where T : UIView
        {
            if (selected != null)
            {
                if (_selectedBtn != null)
                {
                    _selectedBtn.SetStateByManager(NavState.Closed);
                    _selectedBtn = null;
                }

                _selectedBtn = selected;
            }

            Debug.Log($"Typeof: {typeof(T).Name}");
            await UIManager.Instance.TogglePopupAsync<T>(withBackdrop: withBackdrop);
        }

        private void OnClickClose()
        {
            if (_selectedBtn != null)
            {
                _selectedBtn.SetStateByManager(NavState.Closed);
                _selectedBtn = null;
            }

            UIManager.Instance.CloseTopMain();
            TriggerButtonCloseAndGem(false);
        }

        private void RefreshCloseAndGem()
        {
            bool hasAnyMain = UIManager.Instance != null && UIManager.Instance.IsAnyMainVisible();
            TriggerButtonCloseAndGem(hasAnyMain);
        }

        private void TriggerButtonCloseAndGem(bool value)
        {
            ButtonClose.gameObject.SetActive(value);
            Gem.SetActive(!value);
        }
    }
}