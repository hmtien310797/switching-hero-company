using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Shop.Views;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shared.UI
{
    [RequireComponent(typeof(Button))]
    public class UIShopJumpButton : MonoBehaviour
    {
        [SerializeField]
        private Button btn;

        [SerializeField]
        private EShopTab jumpTab;

        private void Awake()
        {
            btn.onClick.AddListener(OnClickJump);
        }

        private void OnDestroy()
        {
            btn.onClick.RemoveListener(OnClickJump);
        }

        private void OnClickJump()
        {
            UIManager.Instance.OpenPopupAsync<ShopView>(new ShopArgs(jumpTab)).Forget();
        }
    }
}