using Immortal_Switch.Scripts.UI;
using Immortal_Switch.Scripts.UI.Shop;
using NaughtyAttributes;
using UnityEngine;

namespace Immortal_Switch.Scripts
{
    public class DebugTesting : MonoBehaviour
    {
        [Button]
        public void OpenShop()
        {
            UIManager.Instance.OpenPopupAsync<ShopView>();
        }

        [Button]
        public void CloseShop()
        {
            UIManager.Instance.Close<ShopView>();
        }
    }
}