using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Event.EventFishing.Layout;
using Immortal_Switch.Scripts.Event.EventFishing.Views;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventFishing
{
    public class EventFishingView : AnimatedUIView
    {
        [Header("Button references")]
        [SerializeField]
        private Button btnBack;

        [SerializeField]
        private Button btnShop;

        [SerializeField]
        private Button btnCollection;

        [Header("Layout references")]
        [SerializeField]
        private UIEventFishingLayoutController layoutVertical;

        private void Awake()
        {
            btnBack.onClick.AddListener(OnClickBack);
            btnCollection.onClick.AddListener(OnClickCollection);
            btnShop.onClick.AddListener(OnClickShop);
        }

        /// <summary>Tải state server (đã câu con nào, số dư mồi) trước khi bind — mọi popup con
        /// (Collection/Shop) đọc qua EventFishingManager.State, không còn random cục bộ.</summary>
        public override async UniTask PlayShowAsync(object args)
        {
            await EventFishingManager.Instance.RefreshAsync();
            await base.PlayShowAsync(args);
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);
            Bind();
        }

        private void OnDestroy()
        {
            btnBack.onClick.RemoveListener(OnClickBack);
            btnCollection.onClick.RemoveListener(OnClickCollection);
            btnShop.onClick.RemoveListener(OnClickShop);
        }

        private void Bind()
        {
            layoutVertical.Bind(OnClickFish);
        }

        private void OnClickFish()
        {
            CastAsync().Forget();
        }

        private async UniTaskVoid CastAsync()
        {
            var (success, result, _, isFirst, error) = await EventFishingManager.Instance.CastAsync();

            if (!success)
            {
                Debug.LogWarning($"[EventFishingView] eventfishing/cast failed: {error}");
                return;
            }

            var eResult = result == "win" ? EEventFishingResult.Win : EEventFishingResult.Lose;

            UIManager.Instance
                .OpenPopupAsync<EventFishingResultView>(new EventFishingResultArgs(eResult, isFirst))
                .Forget();
        }

        private void OnClickCollection()
        {
            UIManager.Instance.TogglePopupAsync<EventFishingCollectionView>().Forget();
        }

        private void OnClickShop()
        {
            UIManager.Instance.TogglePopupAsync<EventFishingShopView>().Forget();
        }

        private void OnClickBack()
        {
            UIManager.Instance.Close<EventFishingView>();
        }
    }
}