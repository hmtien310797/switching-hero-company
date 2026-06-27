using System.Collections.Generic;
using Battle;
using Common;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Equipment.UI;
using Immortal_Switch.Scripts.Equipment.UIRuntime;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Skill.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.UI
{
    public class EquipView : AnimatedUIView
    {
        [SerializeField] private SegmentedControlStatic segmentedControl;
        [SerializeField] private UIWeaponView uiWeaponView;
        [SerializeField] private UISkillView uiSkillView;
        [SerializeField] private WeaponViewDataProvider weaponViewDataProvider;

        private int currentIndex = -1;
        private bool subscribedBattleLineup;
        private UserDataCache userDataCache;
        private EquipViewData equipViewData;

        private void Awake()
        {
            userDataCache = UserDataCache.Instance;
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);

            BindSegmentedControl();
            SubscribeBattleLineupChanged();
            TrySetupSubViews();

            int index = segmentedControl != null ? segmentedControl.GetCurrentIndex() : 0;
            if (index < 0) index = 0;
            
            if (args != null)
            {
                equipViewData = (EquipViewData)args;
                index = (int)equipViewData.Type - 1;
            }

            RefreshViews(index);
        }

        public override void OnHide()
        {
            base.OnHide();
            UnsubscribeBattleLineupChanged();
        }

        private void BindSegmentedControl()
        {
            if (segmentedControl == null)
                return;

            segmentedControl.Initialize(0, OnSegmentChanged);
        }

        private void OnSegmentChanged(int index)
        {
            if (currentIndex == index)
                return;

            RefreshViews(index);
        }

        private void RefreshViews(int index)
        {
            currentIndex = index;

            bool showWeapon = index == 0;
            bool showSkill = index == 1;

            if (uiWeaponView != null)
                uiWeaponView.gameObject.SetActive(showWeapon);

            if (uiSkillView != null)
            {
                if (showSkill && equipViewData is { Type: EquipViewType.SkillView })
                {
                    uiSkillView.SetSelectedHeroClass((HeroClass)equipViewData.Data1);
                }
                uiSkillView.OpenDefaultClass(showSkill);
            }

            if (showWeapon && uiWeaponView != null)
                uiWeaponView.RefreshAll();

        }

        private void TrySetupSubViews()
        {
            if (PvEBattleController.Instance == null)
                return;

            var activeHeroes = userDataCache.inBattleHeroes;
            if (activeHeroes == null || activeHeroes.Length == 0)
                return;

            var focusHero = activeHeroes[0];
            if (focusHero == null)
                return;

            if (uiWeaponView != null && weaponViewDataProvider != null)
            {
                uiWeaponView.Setup(
                    weaponViewDataProvider,
                    focusHero.GetHeroId(),
                    focusHero.HeroClass,
                    new List<HeroActor>(activeHeroes)
                );
            }

            if (uiSkillView != null)
            {
                // nếu UISkillView cần setup theo active lineup thì nối tiếp tại đây
                // uiSkillView.Setup(...);
            }
        }

        private void SubscribeBattleLineupChanged()
        {
            if (subscribedBattleLineup)
                return;

            if (PvEBattleController.Instance == null)
                return;

            GameEventManager.Subscribe(GameEvents.OnActiveLineupChanged, HandleActiveLineupChanged);
            subscribedBattleLineup = true;
        }

        private void UnsubscribeBattleLineupChanged()
        {
            if (!subscribedBattleLineup)
                return;

            GameEventManager.Unsubscribe(GameEvents.OnActiveLineupChanged, HandleActiveLineupChanged);

            subscribedBattleLineup = false;
        }

        private void HandleActiveLineupChanged()
        {
            if (PvEBattleController.Instance == null)
                return;

            var activeHeroes = userDataCache.inBattleHeroes;
            if (activeHeroes == null || activeHeroes.Length == 0)
                return;

            var focusHero = activeHeroes[0];
            if (focusHero == null)
                return;

            if (uiWeaponView != null)
            {
                uiWeaponView.SetDeployedHeroes(new List<HeroActor>(activeHeroes));
                uiWeaponView.SetFocusedHero(focusHero.GetHeroId(), focusHero.HeroClass);
            }

            if (currentIndex == 0 && uiWeaponView != null)
                uiWeaponView.RefreshAll();

            if (currentIndex == 1 && uiSkillView != null)
            {
                // nếu UISkillView có refresh lineup riêng thì gọi ở đây
                // uiSkillView.RefreshAll();
            }
        }
    }

    public class EquipViewData
    {
        public EquipViewType Type { get; set; }
        public object Data1 { get; set; }
        public object Data2 { get; set; }
    }

    public enum EquipViewType
    {
        None = 0,
        WeaponView = 1,
        SkillView = 2,
    }
}