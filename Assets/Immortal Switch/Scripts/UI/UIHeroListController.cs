using UnityEngine;

namespace Immortal_Switch.Scripts.UI
{
    public class UIHeroListController : MonoBehaviour
    {
        // [SerializeField] UIHeroNode node;
        //
        // Dictionary<int, UIHeroNode> heroDicts = new Dictionary<int, UIHeroNode>();
        // private UIHeroNode selectedNode = null;
        // private UIHeroNode lockedNode = null;
        //
        // public void InitList(HeroDataOwn heroIds, int selectedId, int lockId)
        // {
        //     var hCount = heroIds.OwnedHeros.Count;
        //     for (int i = 0; i < hCount; i++)
        //     {
        //         var hid = heroIds.OwnedHeros[i].Id;
        //         var hn = Instantiate(node, transform);
        //         var hd = MasterDataCache.Instance.GetHeroDataById(hid);
        //         var isSelected = selectedId == hid;
        //         if (isSelected)
        //         {
        //             selectedNode = hn;
        //         }
        //         
        //         hn.InitializedNode(hd, isSelected, SelectedNode);
        //         if (lockId == hid)
        //         {
        //             lockedNode = hn;
        //             lockedNode.SetStateLock(true);
        //         }
        //         else
        //             hn.SetStateLock(false);
        //
        //         hn.gameObject.SetActive(true);
        //         heroDicts[hid] = hn;
        //     }
        // }
        //
        // private void SelectedNode(UIHeroNode uhn)
        // {
        //     DeselectOthers(uhn);
        //     selectedNode = uhn;
        //     selectedNode.SetStateNodeSelected(true);
        // }
        //
        // private void DeselectOthers(UIHeroNode uhn)
        // {
        //     if (uhn == selectedNode) return;
        //
        //     selectedNode.SetStateNodeSelected(false);
        // }
        //
        // public void PresetSelectedNode(int selectedId, int lockId)
        // {
        //     var uhn = heroDicts[selectedId];
        //     SelectedNode(uhn);
        //     lockedNode.SetStateLock(false);
        //     lockedNode = heroDicts[lockId];
        //     lockedNode.SetStateLock(true);
        // }
        //
        // public HeroDataSO GetSelectedHero()
        // {
        //     return selectedNode.NodeData;
        // }
    }
}
