using UnityEngine;

namespace Immortal_Switch.Scripts.UI
{
    public class TopMainView : UIView
    {
        public static TopMainView Instance;

        [SerializeField] BattleResultController battleResultController;
        [SerializeField] BattleTimerController battleTimerController;
        [SerializeField] CurrencyView currencyView;

        private void Awake()
        {
            Instance = this;
        }

        public BattleResultController GetBattleResultIntance()
        {
            return battleResultController;
        }

        public BattleTimerController GetBattleTimerIntance()
        {
            return battleTimerController;
        }

        public CurrencyView CurrencyView => currencyView;
    }
}
