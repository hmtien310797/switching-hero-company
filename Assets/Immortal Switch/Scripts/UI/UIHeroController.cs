using Immortal_Switch.Scripts.Core;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    public class UIHeroController : MonoBehaviour
    {
        // public static UIHeroController Instance;
        //
        // [SerializeField] Button exit;
        // [SerializeField] Button cancelBtn;
        // [SerializeField] Button okBtn;
        // [SerializeField] UIHeroNode mainHeroBtn;
        // [SerializeField] UIHeroNode subHeroBtn;
        // [SerializeField] UIHeroListController heroListController;
        //
        // private int selectedFirst = -1;
        // private int selectedSecond = -1;
        // private int curSelectedId = -1;
        //
        // private void Awake()
        // {
        //     Instance = this;
        //
        //     exit?.onClick.AddListener(() => gameObject.SetActive(false));
        //
        //     cancelBtn?.onClick.AddListener(() =>
        //     {
        //         
        //     });
        //
        //     okBtn?.onClick.AddListener(() =>
        //     {
        //         var hd = heroListController.GetSelectedHero();
        //         if (hd.Id == curSelectedId) return;
        //
        //         if (curSelectedId == selectedFirst)
        //         {
        //             GameEventManager.Trigger(GameEvents.OnChangeHero, curSelectedId, hd.Id);
        //             selectedFirst = hd.Id;
        //             mainHeroBtn.InitializedNode(MasterDataCache.Instance.GetHeroDataById(selectedFirst), true, MainSelectedAction);
        //             gameObject.SetActive(false);
        //         }
        //         else
        //         {
        //             GameEventManager.Trigger(GameEvents.OnChangeHero, curSelectedId, hd.Id);
        //             selectedSecond = hd.Id;
        //             subHeroBtn.InitializedNode(MasterDataCache.Instance.GetHeroDataById(selectedSecond), true, SubSelectedAction);
        //             gameObject.SetActive(false);
        //         }
        //         curSelectedId = hd.Id;
        //     });
        // }
        //
        // private void Start()
        // {
        //     gameObject.SetActive(false);
        // }
        //
        // public void OpenUIHero()
        // {
        //     if (selectedFirst > 0)
        //     {
        //         gameObject.SetActive(true);
        //         return;
        //     }
        //
        //     var ownedHeroIds = UserDataCache.Instance.SelectedHeros;
        //     selectedFirst = ownedHeroIds.MainHeroId;
        //     curSelectedId = selectedFirst;
        //     mainHeroBtn.InitializedNode(MasterDataCache.Instance.GetHeroDataById(selectedFirst), true, MainSelectedAction);
        //     selectedSecond = ownedHeroIds.SubHeroId;
        //     subHeroBtn.InitializedNode(MasterDataCache.Instance.GetHeroDataById(selectedSecond), false, SubSelectedAction);
        //     heroListController.InitList(UserDataCache.Instance.OwnedHeroData, selectedFirst, selectedSecond);
        //     gameObject.SetActive(true);
        // }
        //
        // private void MainSelectedAction(UIHeroNode uhn)
        // {
        //     subHeroBtn.SetStateNodeSelected(false);
        //     heroListController.PresetSelectedNode(selectedFirst, selectedSecond);
        //     curSelectedId = selectedFirst;
        // }
        //
        // private void SubSelectedAction(UIHeroNode uhn)
        // {
        //     mainHeroBtn.SetStateNodeSelected(false);
        //     heroListController.PresetSelectedNode(selectedSecond, selectedFirst);
        //     curSelectedId = selectedSecond;
        // }
    }
}
