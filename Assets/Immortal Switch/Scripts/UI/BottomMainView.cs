using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.DungeonSystem.Views;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong;
using Immortal_Switch.Scripts.Event.EventWheel;
using Immortal_Switch.Scripts.GrowthSystem.UI;
using Immortal_Switch.Scripts.HeroUIView;
using Immortal_Switch.Scripts.MissionSystem;
using Immortal_Switch.Scripts.MissionSystem.Views;
using Immortal_Switch.Scripts.SummonSystem.Shared.UI;
using Immortal_Switch.Scripts.TransmutationSystem.Views;
using Immortal_Switch.Scripts.Tutorial;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    public class BottomMainView : MonoBehaviour
    {
        [SerializeField]
        private BottomMainButton ButtonShop;

        [SerializeField]
        private BottomMainButton ButtonHero;

        [SerializeField]
        private BottomMainButton ButtonGrowth;

        [SerializeField]
        private BottomMainButton ButtonEquip;

        [SerializeField]
        private BottomMainButton ButtonMission;

        [SerializeField]
        private BottomMainButton ButtonDungeon;

        [SerializeField]
        private Button ButtonGem;

        [SerializeField]
        private Button ButtonClose;

        [SerializeField]
        private GameObject Gem;

        [SerializeField]
        private GameObject[] disableObjectsWhenPlayDungeon;

        // --- Private Field ---
        private BottomMainButton _selectedBtn;

        private void Awake()
        {
            if (ButtonGrowth != null)
                ButtonGrowth.AddListener(() => OnClickBtnGrowth().Forget());

            if (ButtonEquip != null)
                ButtonEquip.AddListener(() => OnClickBtnEquip().Forget());

            ButtonShop.AddListener(() => OnClickBtnShop().Forget());
            ButtonHero.AddListener(() => OnClickBtnHero().Forget());
            ButtonMission.AddListener(() => OnToggleMain<MissionSystemView>(ButtonMission).Forget());
            ButtonGem.onClick.AddListener(() => OnToggleMain<TransmutationSystemView>(null, false).Forget());
            ButtonDungeon.AddListener(() => OnToggleMain<DungeonMainView>(ButtonDungeon).Forget());

            if (ButtonClose != null)
                ButtonClose.onClick.AddListener(OnClickClose);
        }

        private async UniTask OnClickBtnEquip()
        {
            await OnToggleMain<EquipView>(ButtonEquip);
        }

        private async UniTask OnClickBtnShop()
        {
            await OnToggleMain<SummonHubView>(ButtonShop);
        }

        private async UniTask OnClickBtnHero()
        {
            await OnToggleMain<HeroCollectionView>(ButtonHero);
        }

        private async UniTask OnClickBtnGrowth()
        {
            await OnToggleMain<GrowthView>(ButtonGrowth);
        }

        private void Start()
        {
            TutorialManager.Instance.OnResolveTarget += OnResolveTarget;
            TutorialManager.Instance.OnClick += OnClickTutorial;

            GameEventManager.Subscribe(GameEvents.OnToggleMainView, RefreshCloseAndGem);
            GameEventManager.Subscribe<string>(GameEvents.ON_NAVIGATION_REQUESTED, OnNavigationRequested);
            GameEventManager.Subscribe<bool>(GameEvents.OnPlayDungeon, OnPlayDungeon);
        }

        private void OnDestroy()
        {
            TutorialManager.Instance.OnResolveTarget -= OnResolveTarget;
            TutorialManager.Instance.OnClick -= OnClickTutorial;

            GameEventManager.Unsubscribe(GameEvents.OnToggleMainView, RefreshCloseAndGem);
            GameEventManager.Unsubscribe<string>(GameEvents.ON_NAVIGATION_REQUESTED, OnNavigationRequested);
            GameEventManager.Unsubscribe<bool>(GameEvents.OnPlayDungeon, OnPlayDungeon);
        }

        /// <summary>
        /// Đồng bộ trạng thái bottom button khi NavigationService điều hướng theo event key.
        /// </summary>
        private void OnNavigationRequested(string eventKey)
        {
            BottomMainButton targetButton = null;

            switch (eventKey)
            {
                case EventKeys.EVENT_HERO_SUMMON:
                    targetButton = ButtonShop;
                    break;

                case EventKeys.EVENT_HERO_LEVELUP:
                case EventKeys.EVENT_OWN_HERO:
                case EventKeys.EVENT_REACH_POWER:
                    targetButton = ButtonHero;
                    break;

                case EventKeys.EVENT_EQUIP_ITEM:
                case EventKeys.EVENT_ENHANCE_GEAR:
                case EventKeys.EVENT_FORGE_GEAR:
                case EventKeys.EVENT_SKILL_UPGRADE:
                    targetButton = ButtonEquip;
                    break;

                case EventKeys.EVENT_DUNGEON_CLEAR:
                    targetButton = ButtonDungeon;
                    break;
            }

            if (targetButton == null)
            {
                TriggerButtonCloseAndGem(false);
                return;
            }

            if (_selectedBtn != null &&
                _selectedBtn != targetButton)
            {
                _selectedBtn.SetStateByManager(NavState.Closed);
            }

            _selectedBtn = targetButton;

            _selectedBtn.SetStateByManager(NavState.Hover);
        }

        private void OnPlayDungeon(bool result)
        {
            for (int i = 0; i < disableObjectsWhenPlayDungeon.Length; i++)
            {
                GameObject currentGameObject = disableObjectsWhenPlayDungeon[i];
                currentGameObject.SetActive(!result);
            }
        }

        private async UniTask OnClickTutorial(string arg1, int arg2)
        {
            switch (arg2)
            {
                case 16:
                case 36:
                    await OnClickBtnShop();
                    break;

                case 22:
                    await OnClickBtnHero();
                    break;

                case 28:
                    await OnToggleMain<GrowthView>(ButtonGrowth);
                    break;

                case 32:
                    OnClickClose();
                    break;

                case 42:
                    await OnClickBtnEquip();
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

            var eventWheelView = UIManager.Instance.Get<EventWheelView>();

            if (eventWheelView != null &&
                eventWheelView.IsRolling)
            {
                UIManager.Instance.ShowToast("Vòng quay đang xoay");
                return;
            }

            await UIManager.Instance.TogglePopupAsync<T>(withBackdrop: withBackdrop);
        }

        private void OnClickClose()
        {
            var eventWheelView = UIManager.Instance.Get<EventWheelView>();

            if (eventWheelView != null &&
                eventWheelView.IsRolling)
            {
                UIManager.Instance.ShowToast("Vòng quay đang xoay");
                return;
            }

            if (_selectedBtn != null)
            {
                _selectedBtn.SetStateByManager(NavState.Closed);
                _selectedBtn = null;
            }

            UIManager.Instance.CloseTopMain();
            RefreshCloseAndGem();
        }

        private void RefreshCloseAndGem()
        {
            if (UIManager.Instance != null)
            {
                bool hasAnyMain = UIManager.Instance.IsAnyMainVisible();
                TriggerButtonCloseAndGem(hasAnyMain);

                var hasMainEventLeHoiBangLongVisible = UIManager.Instance.IsOpen<EventLeHoiBangLongView>();

                if (hasMainEventLeHoiBangLongVisible)
                {
                    ButtonClose.gameObject.SetActive(false);
                    Gem.SetActive(false);
                }
            }
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