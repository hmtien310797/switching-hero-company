using System;
using Game.Configs.Generated;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventFishing.UI
{
    public class UIEventFishingShopItem : MonoBehaviour
    {
        [SerializeField]
        private Button btnBuy;

        // --- Private Fields ---
        private Action<DynamicHeroesGlobalSpecificationsEventFishingShopRow> _onClickBuy;
        private DynamicHeroesGlobalSpecificationsEventFishingShopRow _row;

        private void Awake()
        {
            btnBuy.onClick.AddListener(OnClickBuy);
        }

        private void OnDestroy()
        {
            btnBuy.onClick.RemoveListener(OnClickBuy);
        }

        private void OnClickBuy()
        {
            if (_row != null)
            {
                _onClickBuy?.Invoke(_row);
            }
            else
            {
                Debug.LogWarning("[UIEventFishingShopItem] row null");
            }
        }

        public void Bind(
            DynamicHeroesGlobalSpecificationsEventFishingShopRow row,
            Action<DynamicHeroesGlobalSpecificationsEventFishingShopRow> onClickBuy
        )
        {
            _row = row;
            _onClickBuy = onClickBuy;
        }
    }
}