using System;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventFishing.Layout
{
    public class UIEventFishingLayoutController : MonoBehaviour
    {
        [SerializeField]
        private Button btnFish;

        [SerializeField]
        private TextMeshProUGUI txtQuantity;

        // --- Private Fields ---
        private Action _onClickFish;

        private void Awake()
        {
            CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
            btnFish.onClick.AddListener(OnClickFish);
        }

        private void OnClickFish()
        {
            _onClickFish?.Invoke();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnDestroy()
        {
            CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
            btnFish.onClick.RemoveListener(OnClickFish);
        }

        private void HandleCurrencyChanged(CurrencyChangedArgs args)
        {
            Refresh();
        }

        public void Bind(Action onClickFish)
        {
            _onClickFish = onClickFish;
        }

        private void Refresh()
        {
            var quantity = ItemsManager.Instance.GetQuantity(ECurrencyType.fishing_food);
            txtQuantity.text = $"X{quantity}";
        }
    }
}