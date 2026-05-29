using System.Numerics;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.GrowthSystem.UI;
using Immortal_Switch.Scripts.HeroUIView;
using Immortal_Switch.Scripts.Skill.UI;
using Immortal_Switch.Scripts.SummonSystem.Shared.UI;
using Immortal_Switch.Scripts.TransmutationSystem;
using Immortal_Switch.Scripts.TransmutationSystem.UI;
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

            // ButtonGem.onClick.AddListener(() => OnToggleMain<TransmutationSystemView>().Forget());

            if (ButtonClose != null)
                ButtonClose.onClick.AddListener(OnClickClose);

            TransmutationSystemManager.Instance.OnEnergyChanged += OnTransmutationSystemEnergyChanged;
        }

        private void OnTransmutationSystemEnergyChanged(BigInteger obj)
        {
            txtEnergy.SetText(TransmutationSystemHelper.Format(obj));
        }

        private void OnDestroy()
        {
            TransmutationSystemManager.Instance.OnEnergyChanged -= OnTransmutationSystemEnergyChanged;
        }

        private void OnEnable()
        {
            RefreshCloseAndGem();
        }

        private async UniTaskVoid OnToggleMain<T>(BottomMainButton selected) where T : UIView
        {
            if (_selectedBtn != null)
            {
                _selectedBtn.SetStateByManager(NavState.Closed);
                _selectedBtn = null;
            }

            _selectedBtn = selected;

            // Toggle on Main layer:
            // - If opening a PageExclusive => UIManager will close Storm stack + close current page (Rule B)
            // - If closing current active => it will hide and then main backdrop off if none left
            await UIManager.Instance.TogglePopupAsync<T>();

            RefreshCloseAndGem();
        }

        private void OnClickClose()
        {
            if (_selectedBtn != null)
            {
                _selectedBtn.SetStateByManager(NavState.Closed);
                _selectedBtn = null;
            }

            // Close top-most MAIN view:
            // - closes Stackable first (Storm)
            // - then closes PageExclusive (Dungeon/Equip/...)
            UIManager.Instance.CloseTopMain();
            RefreshCloseAndGem();
        }

        private void RefreshCloseAndGem()
        {
            bool hasAnyMain = UIManager.Instance != null && UIManager.Instance.IsAnyMainVisible();

            if (ButtonClose != null)
                ButtonClose.gameObject.SetActive(hasAnyMain);

            if (Gem != null)
                Gem.SetActive(!hasAnyMain);
        }
    }
}