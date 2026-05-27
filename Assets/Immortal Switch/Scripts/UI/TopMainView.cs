using Battle;
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
        
        public HeroSkillBarUI HeroSkillBarUI => heroSkillBarUI;

        private void Awake()
        {
            Instance = this;

            if (switchMainSubHeroButton != null)
                switchMainSubHeroButton.onClick.AddListener(OnSwitchMainSubHeroButtonClicked);
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
