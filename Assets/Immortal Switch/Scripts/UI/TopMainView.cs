using System;
using Battle;
using DG.Tweening;
using Immortal_Switch.Scripts.UI.Skill;
using Immortal_Switch.Scripts.Hero;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    public class TopMainView : UIView
    {
        public static TopMainView Instance;

        [SerializeField] BattleResultController battleResultController;
        [SerializeField] BattleTimerController battleTimerController;
        [SerializeField] HeroSkillBarUI heroSkillBarUI;
        [SerializeField] private Button switchMainSubHeroButton;
        [SerializeField] CurrencyView currencyView;
        [SerializeField] HeroJoystick heroJostick;
        [SerializeField] private Button autoSkillButton;
        [SerializeField] private GameObject rotateObject;
        
        public HeroSkillBarUI HeroSkillBarUI => heroSkillBarUI;
        private bool isAutoActived = false;

        private void Awake()
        {
            Instance = this;

            if (switchMainSubHeroButton != null)
                switchMainSubHeroButton.onClick.AddListener(OnSwitchMainSubHeroButtonClicked);
        }

        private void Start()
        {
            autoSkillButton.onClick.AddListener(()=>
            {
                isAutoActived = !isAutoActived;
                PvEBattleController.Instance.SetAutoSkill(isAutoActived);
                rotateObject.transform.DOLocalRotate(new Vector3(0, 0, isAutoActived ? 180 : 0), 0.2f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1, LoopType.Incremental);
            });

            SetHeroTeamController(HeroTeamController.Instance);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (switchMainSubHeroButton != null)
                switchMainSubHeroButton.onClick.RemoveListener(OnSwitchMainSubHeroButtonClicked);
        }

        private void OnSwitchMainSubHeroButtonClicked()
        {
            PvEBattleController.Instance?.OnSwitchMainSubHeroButtonClicked();
        }

        public void SetHeroTeamController(HeroTeamController heroTeamController)
        {
            heroJostick.SetTarget(heroTeamController);
        }

        public CurrencyView CurrencyView => currencyView;
    }
}
