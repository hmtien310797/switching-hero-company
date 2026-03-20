using Scripts.UI;
using UnityEngine;

public class TopMainView : UIView
{
    public static TopMainView Instance;

    [SerializeField] BattleResultController battleResultController;
    [SerializeField] BattleTimerController battleTimerController;

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
}
