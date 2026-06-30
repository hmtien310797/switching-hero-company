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
using Immortal_Switch.Scripts.Tutorial;
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
                ButtonGrowth.AddListener(OnClickBtnGrowth);

            if (ButtonEquip != null)
                ButtonEquip.AddListener(OnClickBtnEquip);

            ButtonShop.AddListener(OnClickBtnShop);
            ButtonHero.AddListener(OnClickBtnHero);
            ButtonMission.AddListener(() => OnToggleMain<MissionSystemView>(ButtonMission).Forget());
            ButtonGem.onClick.AddListener(() => OnToggleMain<TransmutationSystemView>(null, false).Forget());
            ButtonDungeon.AddListener(() => OnToggleMain<DungeonMainView>(ButtonDungeon).Forget());

            if (ButtonClose != null)
                ButtonClose.onClick.AddListener(OnClickClose);
        }

        private void OnClickBtnEquip()
        {
            OnToggleMain<EquipView>(ButtonEquip).Forget();
        }

        private void OnClickBtnShop()
        {
            OnToggleMain<SummonHubView>(ButtonShop).Forget();
        }

        private void OnClickBtnHero()
        {
            OnToggleMain<HeroCollectionView>(ButtonHero).Forget();
        }

        private void OnClickBtnGrowth()
        {
            OnToggleMain<GrowthView>(ButtonGrowth).Forget();
        }

        private void Start()
        {
            TutorialManager.Instance.OnResolveTarget += OnResolveTarget;
            TutorialManager.Instance.OnClick += OnClickTutorial;
            TransmutationSystemManager.Instance.OnChanged += OnTransmutationSystemChanged;
            GameEventManager.Subscribe(GameEvents.OnToggleMainView, RefreshCloseAndGem);
        }

        private async UniTask OnClickTutorial(string arg1, int arg2)
        {
            switch (arg2)
            {
                case 16:
                case 36:
                    OnClickBtnShop();
                    break;

                case 22:
                    OnClickBtnHero();
                    break;

                case 28:
                    await OnToggleMain<GrowthView>(ButtonGrowth);
                    break;

                case 32:
                    OnClickClose();
                    break;

                case 42:
                    OnClickBtnEquip();
                    break;
            }
        }

        private RectTransform OnResolveTarget(string arg1, int arg2)
        {
            switch (arg2)
            {
                case 16:
                case 36:
                    return ButtonShop.transform as RectTransform;

                case 22:
                    UIManager.Instance.Close<SummonHubView>();
                    return ButtonHero.transform as RectTransform;

                case 28:
                    UIManager.Instance.Close<HeroCollectionView>();
                    return ButtonGrowth.transform as RectTransform;

                case 32:
                    return ButtonClose.transform as RectTransform;

                case 42:
                    UIManager.Instance.Close<SummonHubView>();
                    return ButtonEquip.transform as RectTransform;

                default:
                    return null;
            }
        }

        private void OnDestroy()
        {
            TutorialManager.Instance.OnResolveTarget -= OnResolveTarget;
            TutorialManager.Instance.OnClick -= OnClickTutorial;
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

        private async UniTask OnToggleMain<T>([CanBeNull] BottomMainButton selected, bool withBackdrop = true)
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

            if (_selectedBtn != null)
            {
                if (!value)
                {
                    _selectedBtn.SetStateByManager(NavState.Closed);
                    _selectedBtn = null;
                }
                else
                {
                    _selectedBtn.SetStateByManager(NavState.Hover);
                }
            }
        }
    }
}