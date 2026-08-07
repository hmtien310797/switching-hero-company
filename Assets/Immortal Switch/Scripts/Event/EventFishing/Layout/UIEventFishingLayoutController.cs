using System;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Items;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventFishing.Layout
{
    public class UIEventFishingLayoutController : MonoBehaviour
    {
        private const string ANIM_ANGLER_IDLE = "idle";
        private const string ANIM_ANGLER_FISHING = "fishing";

        [SerializeField]
        private SkeletonGraphic skeletonAngler;

        [SerializeField]
        private Button btnFish;

        [SerializeField]
        private TextMeshProUGUI txtQuantity;

        // --- Private Fields ---
        private Action _onClickFish;

        private void Awake()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
            }

            skeletonAngler.AnimationState.Complete += OnAnimationFishingComplete;

            btnFish.onClick.AddListener(OnClickFish);
        }

        private void OnAnimationFishingComplete(TrackEntry trackEntry)
        {
            SetAnimation(ANIM_ANGLER_IDLE, true);
            _onClickFish?.Invoke();
        }

        private void OnClickFish()
        {
            SetAnimation(ANIM_ANGLER_FISHING, false);
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
            }

            skeletonAngler.AnimationState.Complete -= OnAnimationFishingComplete;

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

        private void SetAnimation(string animName, bool loop)
        {
            skeletonAngler.AnimationState.SetAnimation(0, animName, loop);
        }
    }
}