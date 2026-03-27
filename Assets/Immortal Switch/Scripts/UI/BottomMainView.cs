using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.GachaSystem.HeroSummonView;
using Immortal_Switch.Scripts.GrowthSystem.UI;
using Immortal_Switch.Scripts.HeroUIView;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

public class BottomMainView : UIView
{
    [SerializeField] private Button ButtonShop;
    [SerializeField] private Button ButtonHero;
    [SerializeField] private Button ButtonGrowth;
    [SerializeField] private Button ButtonEquip;
    [SerializeField] private Button ButtonMission;
    [SerializeField] private Button ButtonDungeon;
    [SerializeField] private Button ButtonClose;
    [SerializeField] private GameObject Gem;


    private void Awake()
    {
        // Ensure persistent layer (recommended)
        Layer = UILayer.SubMain;

        if (ButtonEquip != null)
            ButtonEquip.onClick.AddListener(() => OnToggleMain<EquipView>().Forget());

        if (ButtonGrowth != null)
            ButtonGrowth.onClick.AddListener(() => OnToggleMain<GrowthView>().Forget());

        // if (ButtonDungeon != null)
        //     ButtonDungeon.onClick.AddListener(() => OnToggleMain<DungeonView>().Forget());


         ButtonShop.onClick.AddListener(() => OnToggleMain<HeroSummonView>().Forget());
         ButtonHero.onClick.AddListener(() => OnToggleMain<HeroCollectionView>().Forget());

        if (ButtonClose != null)
            ButtonClose.onClick.AddListener(OnClickClose);
    }

    private void OnEnable()
    {
        RefreshCloseAndGem();
    }

    private async UniTaskVoid OnToggleMain<T>() where T : UIView
    {
        // Toggle on Main layer:
        // - If opening a PageExclusive => UIManager will close Storm stack + close current page (Rule B)
        // - If closing current active => it will hide and then main backdrop off if none left
        await UIManager.Instance.TogglePopupAsync<T>();

        RefreshCloseAndGem();
    }

    private void OnClickClose()
    {
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